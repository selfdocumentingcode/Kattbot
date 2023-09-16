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

    private static bool HasLegacyUsername(this DiscordUser user)
    {
        return user.Discriminator != "0";
    }

    public static string GetEmojiImageUrl(this DiscordEmoji emoji)
    {
        bool isEmote = emoji.Id != 0;

        return isEmote ? emoji.Url : EmoteHelper.GetExternalEmojiImageUrl(emoji.Name);
    }

    public static string GetMessageWithTextMentions(this DiscordMessage message)
    {
        var newMessageContent = message.Content;

        foreach (var user in message.MentionedUsers)
        {
            var userMentionAsText = user.Mention.Replace("!", string.Empty);
            newMessageContent = newMessageContent.Replace(userMentionAsText, user.GetDisplayName());
        }

        foreach (var role in message.MentionedRoles)
        {
            newMessageContent = newMessageContent.Replace(role.Mention, role.Name);
        }

        foreach (var channel in message.MentionedChannels)
        {
            newMessageContent = newMessageContent.Replace(channel.Mention, $"#{channel.Name}");
        }

        return newMessageContent;
    }

    public static async Task<string?> GetImageUrlFromMessage(this DiscordMessage message)
    {
        var imgUrl = message.GetAttachmentOrStickerImage();

        if (imgUrl != null)
            return imgUrl;

        if (message.ReferencedMessage is not null)
            imgUrl = message.ReferencedMessage.GetAttachmentOrStickerImage();

        if (imgUrl != null)
            return imgUrl;

        var waitTasks = new List<Task<string?>> { message.WaitForEmbedImage() };

        if (message.ReferencedMessage is not null)
            waitTasks.Add(message.ReferencedMessage.WaitForEmbedImage());

        imgUrl = await (await Task.WhenAny(waitTasks));

        return imgUrl;
    }

    private static string? GetAttachmentOrStickerImage(this DiscordMessage message)
    {
        if (message.Attachments.Count > 0)
        {
            var imgAttachment = message.Attachments.Where(a => a.MediaType.StartsWith("image")).FirstOrDefault();

            if (imgAttachment != null)
            {
                return imgAttachment.Url;
            }
        }
        else if (message.Stickers.Count > 0)
        {
            return message.Stickers[0].StickerUrl;
        }

        return null;
    }

    private static async Task<string?> WaitForEmbedImage(this DiscordMessage message)
    {
        const int maxWaitDurationms = 5 * 1000;
        const int delayMs = 100;

        var cts = new CancellationTokenSource(maxWaitDurationms);

        try
        {
            while (!cts.IsCancellationRequested)
            {
                if (message.Embeds.Count > 0)
                {
                    var imgEmbed = message.Embeds.Where(e => e.Type == "image").FirstOrDefault();

                    if (imgEmbed != null)
                    {
                        return imgEmbed.Url.AbsoluteUri;
                    }
                }

                await Task.Delay(delayMs);
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }

        return null;
    }
}
