using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Kattbot.Helpers;

public static class DiscordExtensions
{
    public static string GetNicknameOrUsername(this DiscordUser user)
    {
        string username = user.Username;

        if (user is DiscordMember member)
        {
            username = !string.IsNullOrWhiteSpace(member.Nickname)
                ? member.Nickname
                : member.DisplayName;
        }

        return username;
    }

    public static string GetEmojiImageUrl(this DiscordEmoji emoji)
    {
        bool isEmote = emoji.Id != 0;

        return isEmote ? emoji.Url : EmoteHelper.GetExternalEmojiImageUrl(emoji.Name);
    }

    public static async Task<string?> GetImageUrlFromMessage(this DiscordMessage message)
    {
        var imgUrl = message.GetAttachmentOrStickerImage();

        if (imgUrl != null)
            return imgUrl;

        if (message.ReferencedMessage != null)
            imgUrl = message.ReferencedMessage.GetAttachmentOrStickerImage();

        if (imgUrl != null)
            return imgUrl;

        var waitTasks = new List<Task<string?>> { message.WaitForEmbedImage() };

        if (message.ReferencedMessage != null)
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
