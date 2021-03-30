using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using Kattbot.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kattbot.CommandModules
{
    public static class EmbedBuilderHelper
    {
        public static string EmbedSpacer = "\u200b";

        public static DiscordEmbed BuildSimpleEmbed(string title, string message)
        {
            var eb = new DiscordEmbedBuilder();

            eb.AddField(title, message);

            var result = eb.Build();

            return result;
        }

        public static List<string> FormatEmoteStats(List<EmoteStats> emoteStats)
        {
            var rank = 1;

            var textLines = new List<string>();

            foreach (var emoteUsage in emoteStats)
            {
                var emoteCode = EmoteHelper.BuildEmoteCode(emoteUsage.EmoteId, emoteUsage.IsAnimated);

                textLines.Add($"{rank}.{emoteCode}:{emoteUsage.Usage}");

                rank++;
            }

            return textLines;
        }

        public static List<string> FormatExtendedEmoteStats(List<ExtendedEmoteStats> emoteStats)
        {
            var rank = 1;

            var textLines = new List<string>();

            foreach (var emoteUsage in emoteStats)
            {
                var formattedPercentage = emoteUsage.PercentageOfTotal.ToString("P");

                textLines.Add($"{rank}.{emoteUsage.EmoteCode}:{emoteUsage.Usage} ({formattedPercentage})");

                rank++;
            }

            return textLines;
        }

        public static DiscordEmbed BuildEmbedFromLines(string title, List<string> allLines, int linesPerColumn = 10)
        {
            var columnCount = (int)Math.Ceiling(((double)allLines.Count / linesPerColumn));

            var sbs = new StringBuilder[columnCount];

            for (int i = 0; i < allLines.Count; i++)
            {
                var columnToPushTo = i / linesPerColumn;

                if (sbs[columnToPushTo] == null)
                {
                    sbs[columnToPushTo] = new StringBuilder();
                }

                sbs[columnToPushTo].AppendLine(allLines[i]);
            }

            var eb = new DiscordEmbedBuilder();

            eb.WithTitle(title);

            for (int i = 0; i < sbs.Length; i++)
            {
                var sbString = sbs[i].ToString();

                eb.AddField(EmbedSpacer, sbString, true);
            }

            var result = eb.Build();

            return result;
        }
    }
}
