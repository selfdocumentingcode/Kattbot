using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Common.Models.Emotes
{
    /// <summary>
    /// Emote db record
    /// </summary>
    public partial class EmoteEntity
    {
        public Guid Id { get; set; }
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public ulong MessageId { get; set; }
        public ulong EmoteId { get; set; }
        public string EmoteName { get; set; } = null!;
        public bool EmoteAnimated { get; set; }

        public DateTimeOffset DateTime { get; set; }
        public EmoteSource Source { get; set; }
    }
}
