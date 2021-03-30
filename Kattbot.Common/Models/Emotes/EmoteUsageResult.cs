using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Common.Models.Emotes
{
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
        public string EmoteCode { get; set; } = null!;
        public int Usage { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class EmoteUsageResult
    {
        public EmoteStats EmoteStats { get; set; }
        public List<EmoteUser> EmoteUsers { get; set; }
    }

    public class EmoteUser
    {
        public ulong UserId { get; set; }
        public int Usage { get; set; }
    }

    public class ExtendedEmoteUser
    {
        public ulong UserId { get; set; }
        public int Usage { get; set; }
        public double PercentageOfTotal { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }
}
