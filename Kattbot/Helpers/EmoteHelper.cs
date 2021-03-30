using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Kattbot.Helper
{
    public static class EmoteHelper
    {
        public static readonly Regex EmoteRegex = new Regex(@"<a{0,1}:\w+:\d+>");
        public static readonly Regex EmoteRegexGrouped = new Regex(@"<(a{0,1}):(\w+):(\d+)>");
        public static readonly string EmoteCodeFormat = "<:{0}:{1}>";
        public static readonly string EmoteAnimatedCodeFormat = "<a:{0}:{1}>";
        public static readonly string EmoteNamePlaceholder = "x";

        public static bool MessageContainsEmotes(string message)
        {
            return EmoteRegex.IsMatch(message);
        }

        public static TempEmote? Parse(string emoteString)
        {
            var match = EmoteRegexGrouped.Match(emoteString);

            if (!match.Success)
                return null;

            var groups = match.Groups;
            var isAnimated = !string.IsNullOrWhiteSpace(groups[1].Value);
            var name = groups[2].Value;
            var id = ulong.Parse(groups[3].Value);

            var parsed = new TempEmote()
            {
                Id = id,
                Name = name,
                Animated = isAnimated
            };

            return parsed;
        }

        public static string BuildEmoteCode(ulong emoteId, bool isAnimated)
        {
            if (isAnimated)
            {
                return string.Format(EmoteAnimatedCodeFormat, EmoteNamePlaceholder, emoteId);
            }
            else
            {
                return string.Format(EmoteCodeFormat, EmoteNamePlaceholder, emoteId);
            }
        }

        public static string BuildEmoteCode(ulong emoteId, string emoteName, bool isAnimated)
        {
            if (isAnimated)
            {
                return string.Format(EmoteAnimatedCodeFormat, emoteName, emoteId);
            }
            else
            {
                return string.Format(EmoteCodeFormat, emoteName, emoteId);
            }
        }
    }
}
