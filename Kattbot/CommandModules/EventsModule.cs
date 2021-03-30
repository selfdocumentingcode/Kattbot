using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.Data;
using Kattbot.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [Group("sc")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class EventsModule : BaseCommandModule
    {
        private const string SpeakingClubEventName = "SpeakingClub";
        private readonly BotOptions _options;
        private readonly DateTimeProvider _dateTimeProvider;
        private readonly EventsRepository _eventsRepo;

        public EventsModule(
            IOptions<BotOptions> options, 
            DateTimeProvider dateTimeProvider, 
            EventsRepository eventsRepo)
        {
            _options = options.Value;
            _dateTimeProvider = dateTimeProvider;
            _eventsRepo = eventsRepo;
        }

        [RequireOwner]
        [Command("create")]
        public async Task CreateEvent(CommandContext ctx, [RemainingText] string description)
        {
            var guildId = ctx.Guild.Id;

            description = description.RemoveQuotes();

            await _eventsRepo.CreateEventTemplate(guildId, SpeakingClubEventName, description);

            await ctx.RespondAsync($"Created event {SpeakingClubEventName}");
        }

        [RequireOwner]
        [Command("update")]
        public async Task UpdateEvent(CommandContext ctx, [RemainingText] string description)
        {
            var guildId = ctx.Guild.Id;

            description = description.RemoveQuotes();

            await _eventsRepo.UpdateEventTemplate(guildId, SpeakingClubEventName, description);

            await ctx.RespondAsync($"Updated event {SpeakingClubEventName}");
        }

        [RequireOwner]
        [Command("delete")]
        public async Task DeleteEvent(CommandContext ctx)
        {
            var guildId = ctx.Guild.Id;

            await _eventsRepo.DeleteEventTemplate(guildId, SpeakingClubEventName);

            await ctx.RespondAsync($"Deleted event {SpeakingClubEventName}");
        }

        [RequireOwnerOrFriend]
        [Command("schedule")]
        public async Task ScheduleEvent(CommandContext ctx, [RemainingText] string dateTimeInput)
        {
            var guildId = ctx.Guild.Id;

            dateTimeInput = dateTimeInput.RemoveQuotes();

            DateTimeOffset inputDateTimeNorway;

            try
            {
                inputDateTimeNorway = _dateTimeProvider.ParseAsNorwayDateTimeOffset(dateTimeInput);
            }
            catch (Exception)
            {
                await ctx.RespondAsync("Invalid date");
                return;
            }

            var currentDateTimeOffset = _dateTimeProvider.GetCurrentNorwayDateTimeOffset();

            if (inputDateTimeNorway < currentDateTimeOffset)
            {
                await ctx.RespondAsync("You cannot schedule an event in the past");
                return;
            }

            var eventTemplate = await _eventsRepo.GetEventTemplate(guildId, SpeakingClubEventName);

            var upcomingEvent = await _eventsRepo.GetUpcomingEvent(eventTemplate.Id, currentDateTimeOffset.UtcDateTime);

            if (upcomingEvent == null)
            {
                var createdEvent = await _eventsRepo.CreateEvent(eventTemplate.Id, inputDateTimeNorway.UtcDateTime);

                var dateAsNorwegianDate = _dateTimeProvider.ConvertDateTimeUtcToNorway(createdEvent.DateTime);
                var formattedDate = _dateTimeProvider.FormatDateTimeOffsetToIso(dateAsNorwegianDate);

                await ctx.RespondAsync($"Scheduled new event on {formattedDate}");
            }
            else
            {
                await _eventsRepo.UpdateEvent(upcomingEvent, inputDateTimeNorway.UtcDateTime);

                var dateAsNorwegianDate = _dateTimeProvider.ConvertDateTimeUtcToNorway(upcomingEvent.DateTime);
                var formattedDate = _dateTimeProvider.FormatDateTimeOffsetToIso(dateAsNorwegianDate);

                await ctx.RespondAsync($"Updated event schedule date to {formattedDate}");
            }
        }

        [RequireOwnerOrFriend]
        [Command("cancel")]
        public async Task CancelEvent(CommandContext ctx)
        {
            var guildId = ctx.Guild.Id;

            var currentDateTimeUtc = _dateTimeProvider.GetCurrentUtcDateTime();

            var eventTemplate = await _eventsRepo.GetEventTemplate(guildId, SpeakingClubEventName);

            var upcomingEvent = await _eventsRepo.GetUpcomingEvent(eventTemplate.Id, currentDateTimeUtc);

            if (upcomingEvent == null)
            {
                await ctx.RespondAsync("No upcoming event is scheduled");
            }
            else
            {
                await _eventsRepo.DeleteEvent(upcomingEvent);

                await ctx.RespondAsync("Event canceled");
            }
        }

        [Command("register")]
        public async Task RegisterForEvent(CommandContext ctx, [RemainingText] string info)
        {
            if (string.IsNullOrWhiteSpace(info))
                info = "N/A";

            var user = ctx.User;
            var guildId = ctx.Guild.Id;

            var userId = user.Id;
            var mention = user.Mention;

            var currentDateTimeUtc = _dateTimeProvider.GetCurrentUtcDateTime();

            var eventTemplate = await _eventsRepo.GetEventTemplate(guildId, SpeakingClubEventName);

            var upcomingEvent = await _eventsRepo.GetUpcomingEvent(eventTemplate.Id, currentDateTimeUtc);

            if (upcomingEvent == null)
            {
                await ctx.RespondAsync("No upcoming event");
                return;
            }

            var existingAttendee = await _eventsRepo.GetEventAttendee(upcomingEvent.Id, userId);

            if (existingAttendee != null)
            {
                await ctx.RespondAsync("You have already registered for this event");
                return;
            }

            await _eventsRepo.CreateAttendee(upcomingEvent.Id, userId, info);

            var dateAsNorwegianDate = _dateTimeProvider.ConvertDateTimeUtcToNorway(upcomingEvent.DateTime);
            var formattedDate = _dateTimeProvider.FormatDateTimeOffsetToIso(dateAsNorwegianDate);

            var commandPrefix = _options.CommandPrefix;

            var unregisterCommand = $"{commandPrefix}sc unregister";

            var response = $"{mention}, you have successfully registered for the next Speaking Club taking place on {formattedDate}.";
            response += $"\r\nIf for some reason you can't make it to the event, please unregister using the `{unregisterCommand}` command until an hour before the event.";

            await ctx.RespondAsync(response);
        }

        [Command("unregister")]
        public async Task UnregisterFromEvent(CommandContext ctx)
        {
            var user = ctx.User;
            var guildId = ctx.Guild.Id;

            var userId = user.Id;

            var currentDateTimeUtc = _dateTimeProvider.GetCurrentUtcDateTime();

            var eventTemplate = await _eventsRepo.GetEventTemplate(guildId, SpeakingClubEventName);

            if (eventTemplate == null)
            {
                await ctx.RespondAsync("No event template defined");

                return;
            }

            var upcomingEvent = await _eventsRepo.GetUpcomingEvent(eventTemplate.Id, currentDateTimeUtc);

            if (upcomingEvent == null)
            {
                await ctx.RespondAsync("No upcoming event");
                return;
            }

            var existingAttendee = await _eventsRepo.GetEventAttendee(upcomingEvent.Id, userId);

            if (existingAttendee == null)
            {
                await ctx.RespondAsync("You are not registered for this event");
                return;
            }

            await _eventsRepo.RemoveEventAttendee(existingAttendee);

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode(EmojiMap.WhiteCheckmark));
        }

        [Command("info")]
        public async Task DisplayEventInfo(CommandContext ctx)
        {
            var guildId = ctx.Guild.Id;

            var eventTemplate = await _eventsRepo.GetEventTemplate(guildId, SpeakingClubEventName);

            if (eventTemplate == null)
            {
                await ctx.RespondAsync("No event template defined");
                return;
            }

            var currentDateTimeUtc = _dateTimeProvider.GetCurrentUtcDateTime();

            var sb = new StringBuilder();

            sb.AppendLine($"**Speaking club**");
            sb.AppendLine();
            sb.AppendLine($"{eventTemplate.Description}");
            sb.AppendLine();

            var upcomingEvent = await _eventsRepo.GetUpcomingEvent(eventTemplate.Id, currentDateTimeUtc);

            if (upcomingEvent == null)
            {
                sb.AppendLine($"No event is scheduled at the moment");
            }
            else
            {
                var dateAsNorwegianDate = _dateTimeProvider.ConvertDateTimeUtcToNorway(upcomingEvent.DateTime);
                var formattedDate = _dateTimeProvider.FormatDateTimeOffsetToIso(dateAsNorwegianDate);

                sb.AppendLine($"Next event is scheduled for **{formattedDate}**");
            }

            await ctx.RespondAsync(sb.ToString());
        }

        [Command("attendees")]
        public async Task DisplayEventAttendees(CommandContext ctx)
        {
            var guildId = ctx.Guild.Id;

            var eventTemplate = await _eventsRepo.GetEventTemplate(guildId, SpeakingClubEventName);

            if (eventTemplate == null)
            {
                await ctx.RespondAsync("No event template defined");
                return;
            }

            var currentDateTimeUtc = _dateTimeProvider.GetCurrentUtcDateTime();

            var upcomingEvent = await _eventsRepo.GetUpcomingEvent(eventTemplate.Id, currentDateTimeUtc);

            if (upcomingEvent == null)
            {
                await ctx.RespondAsync("No event is scheduled at the moment");
                return;
            }

            var dateAsNorwegianDate = _dateTimeProvider.ConvertDateTimeUtcToNorway(upcomingEvent.DateTime);
            var formattedDate = _dateTimeProvider.FormatDateTimeOffsetToIso(dateAsNorwegianDate);

            var attendees = await _eventsRepo.GetEventAttendees(upcomingEvent.Id);

            var sb = new StringBuilder();

            sb.AppendLine($"**Speaking club attendees for {formattedDate}**");
            sb.AppendLine();

            if (attendees.Count > 0)
            {
                foreach (var attendee in attendees)
                {
                    DiscordMember user;

                    if (ctx.Guild.Members.ContainsKey(attendee.UserId))
                    {
                        user = ctx.Guild.Members[attendee.UserId];
                    }
                    else
                    {
                        try
                        {
                            user = await ctx.Guild.GetMemberAsync(attendee.UserId);
                        }
                        catch
                        {
                            user = null!;
                        }
                    }

                    if (user != null)
                    {
                        sb.AppendLine($"{user.GetNicknameOrUsername()} - {attendee.Info}");
                    }
                }
            }
            else
            {
                sb.Append("There are currently no registered attendees :(");
            }

            await ctx.RespondAsync(sb.ToString());
        }

        [Command("help")]
        [Description("Help about speaking club")]
        public Task GetHelpSc(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine();
            sb.AppendLine($"`{commandPrefix}sc info`");
            sb.AppendLine($"`{commandPrefix}sc attendees`");
            sb.AppendLine($"`{commandPrefix}sc register [level]`");
            sb.AppendLine($"`{commandPrefix}sc unregister`");
            sb.AppendLine();
            sb.AppendLine($"Register command usage examples:");
            sb.AppendLine($"`{commandPrefix}sc register beginner`");
            sb.AppendLine($"`{commandPrefix}sc register intermediate`");

            var result = FormattedResultHelper.BuildMessage($"Speaking club related commands", sb.ToString());

            return ctx.RespondAsync(result);
        }

        [Command("help-friend")]
        [Description("Help about speaking club friends")]
        public Task GetHelpScFriend(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine();
            sb.AppendLine($"`{commandPrefix}sc schedule [date]`");
            sb.AppendLine($"`{commandPrefix}sc cancel`");
            sb.AppendLine();
            sb.AppendLine($"Register command usage examples:");
            sb.AppendLine($"`{commandPrefix}sc schedule 2030-12-31 15:30`");

            var result = FormattedResultHelper.BuildMessage($"Speaking club friend commands", sb.ToString());

            return ctx.RespondAsync(result);
        }

        [Command("help-admin")]
        [Description("Help about speaking club admin")]
        public Task GetHelpScAdmin(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine();
            sb.AppendLine($"`{commandPrefix}sc create [description]`");
            sb.AppendLine($"`{commandPrefix}sc update [description]`");
            sb.AppendLine($"`{commandPrefix}sc delete`");
            sb.AppendLine();
            sb.AppendLine($"Register command usage examples:");
            sb.AppendLine($"`{commandPrefix}sc create Speaking club is cool`");
            sb.AppendLine($"`{commandPrefix}sc update Speaking club got even cooler`");

            var result = FormattedResultHelper.BuildMessage($"Speaking club admin commands", sb.ToString());

            return ctx.RespondAsync(result);
        }
    }
}
