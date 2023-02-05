using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;

namespace Kattbot.Helpers;

public static class EmoteHelper
{
    public static readonly Regex EmoteRegex = new(@"<a{0,1}:\w+:\d+>");
    public static readonly Regex EmoteRegexGrouped = new(@"<(a{0,1}):(\w+):(\d+)>");
    public static readonly string EmoteCodeFormat = "<:{0}:{1}>";
    public static readonly string EmoteAnimatedCodeFormat = "<a:{0}:{1}>";
    public static readonly string EmoteNamePlaceholder = "x";

    public static bool MessageContainsEmotes(string message)
    {
        return EmoteRegex.IsMatch(message);
    }

    public static TempEmote? Parse(string emoteString)
    {
        Match match = EmoteRegexGrouped.Match(emoteString);

        if (!match.Success)
        {
            return null;
        }

        GroupCollection groups = match.Groups;
        var isAnimated = !string.IsNullOrWhiteSpace(groups[1].Value);
        var name = groups[2].Value;
        var id = ulong.Parse(groups[3].Value);

        var parsed = new TempEmote()
        {
            Id = id,
            Name = name,
            Animated = isAnimated,
        };

        return parsed;
    }

    public static string BuildEmoteCode(ulong emoteId, bool isAnimated)
    {
        return isAnimated
            ? string.Format(EmoteAnimatedCodeFormat, EmoteNamePlaceholder, emoteId)
            : string.Format(EmoteCodeFormat, EmoteNamePlaceholder, emoteId);
    }

    public static string BuildEmoteCode(ulong emoteId, string emoteName, bool isAnimated)
    {
        return isAnimated ? string.Format(EmoteAnimatedCodeFormat, emoteName, emoteId) : string.Format(EmoteCodeFormat, emoteName, emoteId);
    }

    /// <summary>
    /// Not emoji, belongs to guild.
    /// </summary>
    public static bool IsValidEmote(DiscordEmoji emoji, DiscordGuild guild) => guild.Emojis.ContainsKey(emoji.Id);

    /// <summary>
    /// Not emoji, belongs to guild.
    /// </summary>
    public static bool IsValidEmote(EmoteEntity emote, DiscordGuild guild) => guild.Emojis.ContainsKey(emote.EmoteId);

    /// <summary>
    /// Not emoji, belongs to guild.
    /// </summary>
    public static bool IsValidEmote(ulong emojiId, DiscordGuild guild) => guild.Emojis.ContainsKey(emojiId);
}
