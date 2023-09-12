using DSharpPlus.Entities;
using System.Linq;

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

    public static string? GetImageUrlFromMessage(this DiscordMessage message, bool isRootMessage = true)
    {
        if (message.Attachments.Count > 0)
        {
            var imgAttachment = message.Attachments.Where(a => a.MediaType.StartsWith("image")).FirstOrDefault();

            if (imgAttachment != null)
            {
                return imgAttachment.Url;
            }
        }
        else if (message.Embeds.Count > 0)
        {
            var imgEmbed = message.Embeds.Where(e => e.Type == "image").FirstOrDefault();

            if (imgEmbed != null)
            {
                return imgEmbed.Url.AbsoluteUri;
            }
        }
        else if (isRootMessage == true && message.ReferencedMessage != null)
        {
            return GetImageUrlFromMessage(message.ReferencedMessage, false);
        }

        return null;
    }
}
