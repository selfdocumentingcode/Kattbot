using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;

namespace Kattbot.Helpers;

public static class EmoteHelper
{
    private const string EmoteCodeFormat = "<:{0}:{1}>";
    private const string EmoteAnimatedCodeFormat = "<a:{0}:{1}>";
    private const string EmoteNamePlaceholder = "x";

    private static readonly Regex EmoteRegex = new(@"<a{0,1}:\w+:\d+>");
    private static readonly Regex EmoteRegexGrouped = new(@"<(a{0,1}):(\w+):(\d+)>");

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
            ImageUrl = BuildDiscordEmojiUrl(id, isAnimated),
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
        var utf32Encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: false);

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
            string unicodePart = bytesAsString.Substring(i, length: 8)
                .TrimStart('0')
                .ToLower();

            fileNameBuilder.Append(i == 0 ? unicodePart : $"-{unicodePart}");
        }

        var fileName = fileNameBuilder.ToString();

        return $"https://emoji.aranja.com/static/emoji-data/img-twitter-72/{fileName}.png";
    }

    public static List<string> ExtractEmotesFromMessage(string messageText)
    {
        MatchCollection result = EmoteRegex.Matches(messageText);

        List<string> emojiStrings = result.Select(m => m.Value).ToList();

        return emojiStrings;
    }

    private static string BuildDiscordEmojiUrl(ulong id, bool isAnimated)
    {
        return id == 0
            ? throw new InvalidOperationException("Cannot get URL of unicode emojis.")
            : isAnimated
                ? $"https://cdn.discordapp.com/emojis/{id.ToString(CultureInfo.InvariantCulture)}.gif"
                : $"https://cdn.discordapp.com/emojis/{id.ToString(CultureInfo.InvariantCulture)}.png";
    }
}
