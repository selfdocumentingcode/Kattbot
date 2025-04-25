using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Kattbot.Helpers;

public static class DiscordExtensions
{
    public static string GetDisplayName(this DiscordUser user)
    {
        if (user is DiscordMember member)
        {
            return member.DisplayName;
        }

        return user.GlobalName ?? user.Username;
    }

    public static string GetFullUsername(this DiscordUser user)
    {
        return user.HasLegacyUsername() ? $"{user.Username}#{user.Discriminator}" : user.Username;
    }

    public static string GetEmojiImageUrl(this DiscordEmoji emoji)
    {
        bool isEmote = emoji.Id != 0;

        return isEmote ? emoji.Url : EmoteHelper.GetExternalEmojiImageUrl(emoji.Name);
    }

    public static string SubstituteMentions(this DiscordMessage message, string text)
    {
        string newMessageContent = text;

        foreach (DiscordUser user in message.MentionedUsers)
        {
            string userMentionAsText = user.Mention.Replace("!", string.Empty);
            newMessageContent = newMessageContent.Replace(userMentionAsText, user.GetDisplayName());
        }

        foreach (DiscordRole role in message.MentionedRoles)
        {
            newMessageContent = newMessageContent.Replace(role.Mention, role.Name);
        }

        foreach (DiscordChannel channel in message.MentionedChannels)
        {
            newMessageContent = newMessageContent.Replace(channel.Mention, $"#{channel.Name}");
        }

        return newMessageContent;
    }

    public static string SubstituteMentions(this DiscordMessage message)
    {
        string newMessageContent = message.Content;

        foreach (DiscordUser user in message.MentionedUsers)
        {
            string userMentionAsText = user.Mention.Replace("!", string.Empty);
            newMessageContent = newMessageContent.Replace(userMentionAsText, user.GetDisplayName());
        }

        foreach (DiscordRole role in message.MentionedRoles)
        {
            newMessageContent = newMessageContent.Replace(role.Mention, role.Name);
        }

        foreach (DiscordChannel channel in message.MentionedChannels)
        {
            newMessageContent = newMessageContent.Replace(channel.Mention, $"#{channel.Name}");
        }

        return newMessageContent;
    }

    public static async Task<string?> GetImageUrlFromMessage(this DiscordMessage message)
    {
        string? imgUrl = message.GetAttachmentOrStickerImage();

        if (imgUrl != null)
        {
            return imgUrl;
        }

        if (message.ReferencedMessage is not null)
        {
            imgUrl = message.ReferencedMessage.GetAttachmentOrStickerImage();
        }

        if (imgUrl != null)
        {
            return imgUrl;
        }

        var waitTasks = new List<Task<string?>> { message.WaitForEmbedImage() };

        if (message.ReferencedMessage is not null)
        {
            waitTasks.Add(message.ReferencedMessage.WaitForEmbedImage());
        }

        imgUrl = await await Task.WhenAny(waitTasks);

        return imgUrl;
    }

    private static bool HasLegacyUsername(this DiscordUser user)
    {
        return user.Discriminator != "0";
    }

    private static string? GetAttachmentOrStickerImage(this DiscordMessage message)
    {
        if (message.Attachments.Count > 0)
        {
            DiscordAttachment? imgAttachment =
                message.Attachments.FirstOrDefault(a => a.MediaType?.StartsWith("image") ?? false);

            if (imgAttachment != null)
            {
                return imgAttachment.Url;
            }
        }
        else if (message.Stickers?.Count > 0)
        {
            return message.Stickers[0].StickerUrl;
        }

        return null;
    }

    private static async Task<string?> WaitForEmbedImage(this DiscordMessage message)
    {
        const int maxWaitDurationMs = 5 * 1000;
        const int delayMs = 100;

        var cts = new CancellationTokenSource(maxWaitDurationMs);

        try
        {
            while (!cts.IsCancellationRequested)
            {
                if (message.Embeds.Count > 0)
                {
                    DiscordEmbed? imgEmbed = message.Embeds.FirstOrDefault(e => e.Type == "image");

                    if (imgEmbed?.Url != null)
                    {
                        return imgEmbed.Url.AbsoluteUri;
                    }

                    DiscordEmbed? imgAttachmentEmbed = message.Embeds.FirstOrDefault(e => e.Image != null);

                    if (imgAttachmentEmbed?.Image?.Url != null)
                    {
                        return imgAttachmentEmbed.Image.Url.Value.ToString();
                    }
                }

                await Task.Delay(delayMs, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        return null;
    }
}
