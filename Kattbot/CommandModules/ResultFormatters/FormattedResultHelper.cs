using Kattbot.Common.Models.Emotes;
using Kattbot.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kattbot.CommandModules
{
    public static class FormattedResultHelper
    {
        public static List<string> FormatEmoteStats(List<EmoteStats> emoteStats, int rankOffset)
        {
            var rankPadding = emoteStats.Count.ToString().Length;
            var largestUsage = emoteStats.Max(e => e.Usage);
            var usagePadding = largestUsage.ToString().Length;

            var rank = 1 + rankOffset;

            var textLines = new List<string>();

            foreach (var emoteUsage in emoteStats)
            {
                var formattedRank = rank.ToString().PadLeft(rankPadding, ' ');
                var formattedUsage = emoteUsage.Usage.ToString().PadLeft(usagePadding, ' ');

                var emoteCode = EmoteHelper.BuildEmoteCode(emoteUsage.EmoteId, emoteUsage.IsAnimated);

                textLines.Add($"{formattedRank}.`{emoteCode}`:{formattedUsage}");

                rank++;
            }

            return textLines;
        }

        public static List<string> FormatExtendedEmoteStats(List<ExtendedEmoteStats> emoteStats, int rankOffset)
        {
            var rankPadding = emoteStats.Count.ToString().Length;
            var usagePadding = emoteStats.Max(e => e.Usage.ToString().Length);
            var percentagePadding = emoteStats.Max(e => e.PercentageOfTotal.ToString("P").Length);

            var rank = 1 + rankOffset;

            var textLines = new List<string>();

            foreach (var emoteUsage in emoteStats)
            {
                var formattedRank = rank.ToString().PadLeft(rankPadding);
                var formattedUsage = emoteUsage.Usage.ToString().PadLeft(usagePadding);
                var formattedPercentage = emoteUsage.PercentageOfTotal.ToString("P").PadLeft(percentagePadding);

                textLines.Add($"{formattedRank}.`{emoteUsage.EmoteCode}`:{formattedUsage} ({formattedPercentage})");

                rank++;
            }

            return textLines;
        }

        public static List<string> FormatExtendedEmoteUsers(List<ExtendedEmoteUser> emoteUsers, int rankOffset)
        {
            var rankPadding = emoteUsers.Count.ToString().Length;
            var displayNamePadding = emoteUsers.Max(e => e.DisplayName.ToString().Length);
            var usagePadding = emoteUsers.Max(e => e.Usage.ToString().Length);
            var percentagePadding = emoteUsers.Max(e => e.PercentageOfTotal.ToString("P").Length);

            var rank = 1 + rankOffset;

            var textLines = new List<string>();

            foreach (var emoteUser in emoteUsers)
            {
                var formattedRank = rank.ToString().PadLeft(rankPadding);
                var formattedDisplayName = emoteUser.DisplayName.ToString().PadRight(displayNamePadding);
                var formattedUsage = emoteUser.Usage.ToString().PadLeft(usagePadding);
                var formattedPercentage = emoteUser.PercentageOfTotal.ToString("P").PadLeft(percentagePadding);

                textLines.Add($"{formattedRank}. {formattedDisplayName}: {formattedUsage} ({formattedPercentage})");

                rank++;
            }

            return textLines;
        }

        public static string BuildBody(List<string> allLines)
        {
            var sb = new StringBuilder();

            allLines
                .SkipLast(1)
                .ToList()
                .ForEach((l) => sb.AppendLine(l));

            sb.Append(allLines.Last());

            return sb.ToString();
        }

        public static string BuildMessage(string title, string message)
        {
            string result = $"{title}\r\n{message}";

            return result;
        }
    }
}
