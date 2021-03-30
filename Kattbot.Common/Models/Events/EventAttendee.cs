using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Common.Models.Events
{
    public class EventAttendee
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;
        public ulong UserId { get; set; }
        public string Info { get; set; } = null!;

    }
}
