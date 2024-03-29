using System.Text;
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

    public static TempEmote? Parse(string emoteString)
    {
        Match match = EmoteRegexGrouped.Match(emoteString);

        if (!match.Success)
        {
            return null;
        }

        GroupCollection groups = match.Groups;
        bool isAnimated = !string.IsNullOrWhiteSpace(groups[1].Value);
        string name = groups[2].Value;
        ulong id = ulong.Parse(groups[3].Value);

        var parsed = new TempEmote
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
        return isAnimated
            ? string.Format(EmoteAnimatedCodeFormat, emoteName, emoteId)
            : string.Format(EmoteCodeFormat, emoteName, emoteId);
    }

    /// <summary>
    ///     Not emoji, belongs to guild.
    /// </summary>
    public static bool IsValidEmote(DiscordEmoji emoji, DiscordGuild guild)
    {
        return guild.Emojis.ContainsKey(emoji.Id);
    }

    /// <summary>
    ///     Not emoji, belongs to guild.
    /// </summary>
    public static bool IsValidEmote(EmoteEntity emote, DiscordGuild guild)
    {
        return guild.Emojis.ContainsKey(emote.EmoteId);
    }

    public static string GetExternalEmojiImageUrl(string code)
    {
        // eggplant = 0001F346
        // https://emoji.aranja.com/static/emoji-data/img-twitter-72/1f346.png

        // flag =  0001F1E6 0001F1E9
        // https://emoji.aranja.com/static/emoji-data/img-twitter-72/1f1e6-1f1e9.png
        var utf32Encoding = new UTF32Encoding(true, false);

        byte[] bytes = utf32Encoding.GetBytes(code);

        var sb = new StringBuilder();
        for (var i = 0; i < bytes.Length; i++)
        {
            sb.AppendFormat("{0:X2}", bytes[i]);
        }

        var bytesAsString = sb.ToString();

        var fileNameBuilder = new StringBuilder();

        for (var i = 0; i < bytesAsString.Length; i += 8)
        {
            string unicodePart = bytesAsString.Substring(i, 8)
                .TrimStart('0')
                .ToLower();

            fileNameBuilder.Append(i == 0 ? unicodePart : $"-{unicodePart}");
        }

        var fileName = fileNameBuilder.ToString();

        return $"https://emoji.aranja.com/static/emoji-data/img-twitter-72/{fileName}.png";
    }
}
