using System.Collections.Generic;

namespace Kattbot.Common.Models.Emotes;

public class EmoteStats
{
    public ulong EmoteId { get; set; }

    public bool IsAnimated { get; set; }

    public int Usage { get; set; }
}

public class ExtendedStatsQueryResult
{
    public ulong EmoteId { get; set; }

    public bool IsAnimated { get; set; }

    public int Usage { get; set; }

    public int TotalUsage { get; set; }
}

public class ExtendedEmoteStats
{
    public string EmoteCode { get; init; } = null!;

    public int Usage { get; init; }

    public double PercentageOfTotal { get; init; }
}

public class EmoteUsageResult
{
    public EmoteStats? EmoteStats { get; init; }

    public List<EmoteUser> EmoteUsers { get; init; } = [];
}

public class EmoteUser
{
    public ulong UserId { get; init; }

    public int Usage { get; init; }
}

public class ExtendedEmoteUser
{
    public ulong UserId { get; init; }

    public int Usage { get; init; }

    public double PercentageOfTotal { get; init; }

    public string DisplayName { get; set; } = string.Empty;
}
