using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kattbot.Common.Models.Emotes;
using Kattbot.Helpers;

namespace Kattbot.CommandModules.ResultFormatters;

public static class FormattedResultHelper
{
    public static List<string> FormatEmoteStats(List<EmoteStats> emoteStats, int rankOffset)
    {
        int rankPadding = emoteStats.Count.ToString().Length;
        int largestUsage = emoteStats.Max(e => e.Usage);
        int usagePadding = largestUsage.ToString().Length;

        int rank = 1 + rankOffset;

        var textLines = new List<string>();

        foreach (EmoteStats emoteUsage in emoteStats)
        {
            string formattedRank = rank.ToString().PadLeft(rankPadding, ' ');
            string formattedUsage = emoteUsage.Usage.ToString().PadLeft(usagePadding, ' ');

            string emoteCode = EmoteHelper.BuildEmoteCode(emoteUsage.EmoteId, emoteUsage.IsAnimated);

            textLines.Add($"{formattedRank}.`{emoteCode}`:{formattedUsage}");

            rank++;
        }

        return textLines;
    }

    public static List<string> FormatExtendedEmoteStats(List<ExtendedEmoteStats> emoteStats, int rankOffset)
    {
        int rankPadding = emoteStats.Count.ToString().Length;
        int usagePadding = emoteStats.Max(e => e.Usage.ToString().Length);
        int percentagePadding = emoteStats.Max(e => e.PercentageOfTotal.ToString("P").Length);

        int rank = 1 + rankOffset;

        var textLines = new List<string>();

        foreach (ExtendedEmoteStats emoteUsage in emoteStats)
        {
            string formattedRank = rank.ToString().PadLeft(rankPadding);
            string formattedUsage = emoteUsage.Usage.ToString().PadLeft(usagePadding);
            string formattedPercentage = emoteUsage.PercentageOfTotal.ToString("P").PadLeft(percentagePadding);

            textLines.Add($"{formattedRank}.`{emoteUsage.EmoteCode}`:{formattedUsage} ({formattedPercentage})");

            rank++;
        }

        return textLines;
    }

    public static List<string> FormatExtendedEmoteUsers(List<ExtendedEmoteUser> emoteUsers, int rankOffset)
    {
        int rankPadding = emoteUsers.Count.ToString().Length;
        int displayNamePadding = emoteUsers.Max(e => e.DisplayName.ToString().Length);
        int usagePadding = emoteUsers.Max(e => e.Usage.ToString().Length);
        int percentagePadding = emoteUsers.Max(e => e.PercentageOfTotal.ToString("P").Length);

        int rank = 1 + rankOffset;

        var textLines = new List<string>();

        foreach (ExtendedEmoteUser emoteUser in emoteUsers)
        {
            string formattedRank = rank.ToString().PadLeft(rankPadding);
            string formattedDisplayName = emoteUser.DisplayName.ToString().PadRight(displayNamePadding);
            string formattedUsage = emoteUser.Usage.ToString().PadLeft(usagePadding);
            string formattedPercentage = emoteUser.PercentageOfTotal.ToString("P").PadLeft(percentagePadding);

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
