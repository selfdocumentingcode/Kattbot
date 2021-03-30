using System;
using System.Collections.Generic;
using System.Text;

namespace Kattbot.Common.Models.Events
{
    public class Event
    {
        public Guid Id { get; set; }
        public Guid EventTemplateId { get; set; }
        public EventTemplate EventTemplate { get; set; } = null!;
        public DateTime DateTime { get; set; }

        public virtual IEnumerable<EventAttendee> EventAttendees { get; set; }

        public Event()
        {
            EventAttendees = new List<EventAttendee>();
        }
    }
}
