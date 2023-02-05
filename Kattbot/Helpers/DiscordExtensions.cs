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
}
