using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Common.Models.Events
{
    public class EventTemplate
    {
        public Guid Id { get; set; }
        public ulong GuildId { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}
