using Kattbot.Common.Models.Events;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Data
{
    public class EventsRepository
    {
        private readonly KattbotContext _dbContext;

        public EventsRepository(KattbotContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Guid> CreateEventTemplate(ulong guildId, string name, string description)
        {
            var existingEventTemplate = await _dbContext.EventTemplates.AsQueryable()
                .FirstOrDefaultAsync(e => e.GuildId == guildId
                                          && e.Name == name);

            if (existingEventTemplate != null)
            {
                throw new Exception($"Event template {name} already exists");
            }

            var newEventTemplate = new EventTemplate()
            {
                GuildId = guildId,
                Name = name,
                Description = description
            };

            await _dbContext.AddAsync(newEventTemplate);
            await _dbContext.SaveChangesAsync();

            return newEventTemplate.Id;
        }

        public async Task<Guid> UpdateEventTemplate(ulong guildId, string name, string description)
        {
            var existingEventTemplate = await _dbContext.EventTemplates.AsQueryable()
                .FirstOrDefaultAsync(e => e.GuildId == guildId
                                          && e.Name == name);

            if (existingEventTemplate == null)
            {
                throw new Exception($"Event template {name} does not exist");
            }

            existingEventTemplate.Description = description;

            await _dbContext.SaveChangesAsync();

            return existingEventTemplate.Id;
        }

        public async Task DeleteEventTemplate(ulong guildId, string name)
        {
            var existingEventTemplate = await _dbContext.EventTemplates.AsQueryable()
                .FirstOrDefaultAsync(e => e.GuildId == guildId
                                          && e.Name == name);

            if (existingEventTemplate == null)
            {
                throw new Exception($"Event template {name} does not exist");
            }

            _dbContext.Remove(existingEventTemplate);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<EventTemplate> GetEventTemplate(ulong guildId, string name)
        {
            var eventTemplate = await _dbContext.EventTemplates.AsQueryable()
                            .FirstOrDefaultAsync(e => e.GuildId == guildId
                                                   && e.Name == name);

            if (eventTemplate == null)
            {
                throw new Exception($"Event template {name} does not exist");
            }

            return eventTemplate;
        }

        public async Task<Event> CreateEvent(Guid evenTemplateId, DateTime dateTime)
        {
            var newEvent = new Event()
            {
                EventTemplateId = evenTemplateId,
                DateTime = dateTime
            };

            await _dbContext.AddAsync(newEvent);

            await _dbContext.SaveChangesAsync();

            return newEvent;
        }

        public async Task<Guid> UpdateEvent(Event eventObj, DateTime dateTime)
        {
            eventObj.DateTime = dateTime;

            await _dbContext.SaveChangesAsync();

            return eventObj.Id;
        }

        public async Task<Event> GetUpcomingEvent(Guid eventTemplateId, DateTime currentDateTime)
        {
            var upcomingEvent = await _dbContext.Events.AsQueryable()
                                .Where(e => e.EventTemplateId == eventTemplateId
                                            && e.DateTime > currentDateTime)
                                .FirstOrDefaultAsync();

            return upcomingEvent;
        }

        public async Task DeleteEvent(Event eventObj)
        {
            _dbContext.Remove(eventObj);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<Guid> CreateAttendee(Guid eventId, ulong userId, string info)
        {
            var attendee = new EventAttendee()
            {
                EventId = eventId,
                Info = info,
                UserId = userId
            };

            await _dbContext.AddAsync(attendee);

            await _dbContext.SaveChangesAsync();

            return attendee.Id;
        }

        public async Task<EventAttendee> GetEventAttendee(Guid eventId, ulong userId)
        {
            var attendee = await _dbContext.EventAttendees.AsQueryable()
                            .FirstOrDefaultAsync(ea => ea.EventId == eventId
                                            && ea.UserId == userId);

            return attendee;
        }

        public async Task RemoveEventAttendee(EventAttendee attendee)
        {
            _dbContext.Remove(attendee);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<EventAttendee>> GetEventAttendees(Guid eventId)
        {
            var attendees = await _dbContext.EventAttendees.AsQueryable()
                            .Where(ea => ea.EventId == eventId)
                            .OrderBy(ea => ea.Info)
                            .ToListAsync();

            return attendees;
        }
    }
}
