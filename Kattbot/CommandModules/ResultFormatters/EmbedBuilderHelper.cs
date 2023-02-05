using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using Kattbot.Helpers;

namespace Kattbot.CommandModules.ResultFormatters;

public static class EmbedBuilderHelper
{
    public static string EmbedSpacer = "\u200b";

    public static DiscordEmbed BuildSimpleEmbed(string title, string message)
    {
        var eb = new DiscordEmbedBuilder();

        eb.AddField(title, message);

        DiscordEmbed result = eb.Build();

        return result;
    }

    public static List<string> FormatEmoteStats(List<EmoteStats> emoteStats)
    {
        int rank = 1;

        var textLines = new List<string>();

        foreach (EmoteStats emoteUsage in emoteStats)
        {
            string emoteCode = EmoteHelper.BuildEmoteCode(emoteUsage.EmoteId, emoteUsage.IsAnimated);

            textLines.Add($"{rank}.{emoteCode}:{emoteUsage.Usage}");

            rank++;
        }

        return textLines;
    }

    public static List<string> FormatExtendedEmoteStats(List<ExtendedEmoteStats> emoteStats)
    {
        int rank = 1;

        var textLines = new List<string>();

        foreach (ExtendedEmoteStats emoteUsage in emoteStats)
        {
            string formattedPercentage = emoteUsage.PercentageOfTotal.ToString("P");

            textLines.Add($"{rank}.{emoteUsage.EmoteCode}:{emoteUsage.Usage} ({formattedPercentage})");

            rank++;
        }

        return textLines;
    }

    public static DiscordEmbed BuildEmbedFromLines(string title, List<string> allLines, int linesPerColumn = 10)
    {
        int columnCount = (int)Math.Ceiling((double)allLines.Count / linesPerColumn);

        var sbs = new StringBuilder[columnCount];

        for (int i = 0; i < allLines.Count; i++)
        {
            int columnToPushTo = i / linesPerColumn;

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
            string sbString = sbs[i].ToString();

            eb.AddField(EmbedSpacer, sbString, true);
        }

        DiscordEmbed result = eb.Build();

        return result;
    }
}
