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
    [BaseCommandCheck]
    [Group("sc-admin")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AdminEventsModule : BaseCommandModule
    {
        private const string SpeakingClubEventName = "SpeakingClub";
        private readonly BotOptions _options;
        private readonly DateTimeProvider _dateTimeProvider;
        private readonly EventsRepository _eventsRepo;

        public AdminEventsModule(
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

        [RequireOwnerOrFriend]
        [Command("ping-attendees")]
        public async Task PingEventAttendees(CommandContext ctx)
        {
            var guildId = ctx.Guild.Id;

            var eventTemplate = await _eventsRepo.GetEventTemplate(guildId, SpeakingClubEventName);

            if (eventTemplate == null)
            {
                await ctx.RespondAsync("No event template defined");
                return;
            }

            var currentDateTimeUtc = _dateTimeProvider.GetCurrentUtcDateTime();

            // Fetch event even if it's been schedule 2 hours back
            var timeAdjustedForDuration = currentDateTimeUtc.AddHours(-2);

            var upcomingEvent = await _eventsRepo.GetUpcomingEvent(eventTemplate.Id, timeAdjustedForDuration);

            if (upcomingEvent == null)
            {
                await ctx.RespondAsync("No event is scheduled at the moment");
                return;
            }

            var attendees = await _eventsRepo.GetEventAttendees(upcomingEvent.Id);

            var sb = new StringBuilder();

            if (currentDateTimeUtc > upcomingEvent.DateTime)
            {
                sb.AppendLine($"We're waiting for you");
            }
            else
            {
                sb.AppendLine($"Speaking club is about to start soon");
            }

            var attendeeMentions = new List<string>();

            if (attendees.Count > 0)
            {
                var memberVoiceChannel = ctx.Member.VoiceState.Channel;

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
                        if (memberVoiceChannel == null)
                        {
                            attendeeMentions.Add(user.Mention);
                        }
                        else if (user.VoiceState.Channel == null || user.VoiceState.Channel.Id != memberVoiceChannel?.Id)
                        {
                            attendeeMentions.Add(user.Mention);
                        }
                    }
                }

                sb.AppendLine(string.Join(", ", attendeeMentions));
            }
            else
            {
                sb.Append("There are currently no registered attendees :(");
            }

            await ctx.RespondAsync(sb.ToString());
        }

        [Command("help-friend")]
        [Description("Help about speaking club friends")]
        public Task GetHelpScFriend(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine();
            sb.AppendLine($"`{commandPrefix}sc-admin schedule [date]`");
            sb.AppendLine($"`{commandPrefix}sc-admin cancel`");
            sb.AppendLine($"`{commandPrefix}sc-admin ping-attendees`");
            sb.AppendLine();
            sb.AppendLine($"Register command usage examples:");
            sb.AppendLine($"`{commandPrefix}sc-admin schedule 2030-12-31 15:30`");

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
            sb.AppendLine($"`{commandPrefix}sc-admin create [description]`");
            sb.AppendLine($"`{commandPrefix}sc-admin update [description]`");
            sb.AppendLine($"`{commandPrefix}sc-admin delete`");
            sb.AppendLine();
            sb.AppendLine($"Register command usage examples:");
            sb.AppendLine($"`{commandPrefix}sc-admin create Speaking club is cool`");
            sb.AppendLine($"`{commandPrefix}sc-admin update Speaking club got even cooler`");

            var result = FormattedResultHelper.BuildMessage($"Speaking club admin commands", sb.ToString());

            return ctx.RespondAsync(result);
        }
    }
}
