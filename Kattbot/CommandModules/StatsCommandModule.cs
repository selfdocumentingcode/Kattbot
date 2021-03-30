using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.Common.Models;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Helper;
using Kattbot.Helpers;
using Kattbot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kattbot.CommandModules
{
    [Group("stats")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class StatsCommandModule : BaseCommandModule
    {
        private const int ResultsPerPage = 10;

        private readonly BotOptions _options;
        private readonly EmoteStatsRepository _emoteStatsRepo;

        public StatsCommandModule(
            IOptions<BotOptions> options,
            EmoteStatsRepository emoteStatsRepo)
        {
            _options = options.Value;
            _emoteStatsRepo = emoteStatsRepo;
        }

        [Command("best")]
        [Description("Return best emotes")]
        public async Task GetBestEmotes(CommandContext ctx, [RemainingText] StatsCommandArgs? args = null)
        {
            args ??= new StatsCommandArgs();

            var guildEmotes = ctx.Guild.Emojis;

            await GetRankedEmotes(ctx, SortDirection.DESC, args.Page, args.Interval, guildEmotes);
        }

        [Command("worst")]
        [Description("Return worst emotes")]
        public async Task GetWorstEmotes(CommandContext ctx, [RemainingText] StatsCommandArgs? args = null)
        {
            args ??= new StatsCommandArgs();

            var guildEmotes = ctx.Guild.Emojis;

            await GetRankedEmotes(ctx, SortDirection.ASC, args.Page, args.Interval, guildEmotes);
        }

        private async Task GetRankedEmotes(CommandContext ctx, SortDirection direction, int page, string interval, IReadOnlyDictionary<ulong, DiscordEmoji> guildEmotes)
        {
            var guildId = ctx.Guild.Id;

            DateTime? fromDate;

            try
            {
                var intervalValue = IntervalValue.Parse(interval);

                fromDate = GetDateFromInterval(intervalValue);
            }
            catch (ArgumentException)
            {
                await ctx.RespondAsync("Invalid interval");
                return;
            }

            int pageOffset = page - 1;

            var mappedGuildEmotes = guildEmotes.Select(kv => new TempEmote()
            {
                Id = kv.Key,
                Name = kv.Value.Name,
                Animated = kv.Value.IsAnimated
            }).ToList();

            var emoteUsageResult = await _emoteStatsRepo.GetGuildEmoteStats(guildId, direction, mappedGuildEmotes, pageOffset: pageOffset, perPage: ResultsPerPage, fromDate: fromDate);

            var emoteUsageItems = emoteUsageResult.Items;
            var safePageOffset = emoteUsageResult.PageOffset;
            var pageCount = emoteUsageResult.PageCount;
            var totalCount = emoteUsageResult.TotalCount;

            if (emoteUsageItems.Count == 0)
            {
                await ctx.RespondAsync("No stats yet");
                return;
            }

            var bestOrWorst = direction == SortDirection.ASC ? "worst" : "best";

            string rangeText;

            if (safePageOffset == 0)
            {
                rangeText = ResultsPerPage.ToString();
            }
            else
            {
                var rangeMin = ResultsPerPage * safePageOffset + 1;
                var rangeMax = Math.Min(ResultsPerPage * safePageOffset + ResultsPerPage, totalCount);
                rangeText = $"{rangeMin} - {rangeMax}";
            }

            var title = $"Top {rangeText} {bestOrWorst} emotes";

            if (fromDate != null)
            {
                title += $" from {fromDate.Value:yyyy-MM-dd}";
            }

            var rankOffset = safePageOffset * ResultsPerPage;

            var lines = FormattedResultHelper.FormatEmoteStats(emoteUsageItems, rankOffset);

            var body = FormattedResultHelper.BuildBody(lines);

            var result = new StringBuilder();

            result.AppendLine(title);
            result.AppendLine();
            result.AppendLine(body);

            if (pageCount > 1)
            {
                result.AppendLine();

                var pagingText = $"Page {safePageOffset + 1}/{pageCount}";

                if (safePageOffset + 1 < pageCount)
                {
                    pagingText += $" (use -p {safePageOffset + 2} to view next page)";
                }

                result.AppendLine(pagingText);
            }

            var formattedResultMessage = $"`{result}`";

            await ctx.RespondAsync(formattedResultMessage);
        }

        [Command("me")]
        [Description("Return best emotes for self")]
        public async Task GetBestEmotesSelf(CommandContext ctx, [RemainingText] StatsCommandArgs? args = null)
        {
            args ??= new StatsCommandArgs();

            var userId = ctx.User.Id;

            var mention = ctx.User.GetNicknameOrUsername();

            await GetBestEmotesUser(ctx, userId, mention, args.Page, args.Interval);
        }

        [Command("user")]
        [Description("Return best emotes for user")]
        public async Task GetBestEmotesOtherUser(CommandContext ctx, DiscordUser user, [RemainingText] StatsCommandArgs? args = null)
        {
            args ??= new StatsCommandArgs();

            var userId = user.Id;

            var mention = user.GetNicknameOrUsername();

            await GetBestEmotesUser(ctx, userId, mention, args.Page, args.Interval);
        }

        private async Task GetBestEmotesUser(CommandContext ctx, ulong userId, string mention, int page, string interval)
        {
            var guildId = ctx.Guild.Id;

            DateTime? fromDate;

            try
            {
                var intervalValue = IntervalValue.Parse(interval);

                fromDate = GetDateFromInterval(intervalValue);
            }
            catch (ArgumentException)
            {
                await ctx.RespondAsync("Invalid interval");
                return;
            }

            int pageOffset = page - 1;

            var emoteUsageResult = await _emoteStatsRepo.GetBestEmotesForUser(guildId, userId, pageOffset, ResultsPerPage, fromDate);

            var emoteUsageItems = emoteUsageResult.Items;
            var safePageOffset = emoteUsageResult.PageOffset;
            var pageCount = emoteUsageResult.PageCount;
            var totalCount = emoteUsageResult.TotalCount;

            var emoteUsageList = emoteUsageItems
                .Select(r => new ExtendedEmoteStats()
                {
                    EmoteCode = EmoteHelper.BuildEmoteCode(r.EmoteId, r.IsAnimated),
                    Usage = r.Usage,
                    PercentageOfTotal = (double)r.Usage / r.TotalUsage

                })
                .ToList();

            if (emoteUsageList.Count == 0)
            {
                await ctx.RespondAsync("No stats yet");
                return;
            }

            string rangeText;

            if (safePageOffset == 0)
            {
                rangeText = ResultsPerPage.ToString();
            }
            else
            {
                var rangeMin = ResultsPerPage * safePageOffset + 1;
                var rangeMax = Math.Min(ResultsPerPage * safePageOffset + ResultsPerPage, totalCount);
                rangeText = $"{rangeMin} - {rangeMax}";
            }

            var title = $"Top {rangeText} emotes for {mention}";

            if (fromDate != null)
            {
                title += $" from {fromDate.Value:yyyy-MM-dd}";
            }

            var rankOffset = safePageOffset * ResultsPerPage;

            var lines = FormattedResultHelper.FormatExtendedEmoteStats(emoteUsageList, rankOffset);

            var body = FormattedResultHelper.BuildBody(lines);

            var result = new StringBuilder();

            result.AppendLine(title);
            result.AppendLine();
            result.AppendLine(body);

            if (pageCount > 1)
            {
                result.AppendLine();

                var pagingText = $"Page {safePageOffset + 1}/{pageCount}";

                if (safePageOffset + 1 < pageCount)
                {
                    pagingText += $" (use -p {safePageOffset + 2} to view next page)";
                }

                result.AppendLine(pagingText);
            }

            var formattedResultMessage = $"`{result}`";

            await ctx.RespondAsync(formattedResultMessage);
        }

        [Command("emote")]
        [Description("Return specific emote stats")]
        public async Task GetEmoteStats(CommandContext ctx, string emoteString, [RemainingText] StatsCommandArgs? args = null)
        {
            args ??= new StatsCommandArgs();

            var emoji = EmoteHelper.Parse(emoteString);

            if (emoji != null)
            {
                await GetEmoteStats(ctx, emoji, args.Interval);
            }
            else
            {
                await ctx.RespondAsync("I don't know what to do with this");
            }
        }

        private async Task GetEmoteStats(CommandContext ctx, TempEmote emote, string interval)
        {
            var guild = ctx.Guild;
            var guildId = guild.Id;

            var isValidEmote = IsValidMessageEmote(emote.Id, guild);

            if (!isValidEmote)
            {
                await ctx.RespondAsync("Invalid emote");
                return;
            }

            DateTime? fromDate;

            try
            {
                var intervalValue = IntervalValue.Parse(interval);

                fromDate = GetDateFromInterval(intervalValue);
            }
            catch (ArgumentException)
            {
                await ctx.RespondAsync("Invalid interval");
                return;
            }

            // Maybe replace with pagination
            const int maxUserCount = 10;

            var emoteUsageResult = await _emoteStatsRepo.GetSingleEmoteStats(guildId, emote, maxUserCount, fromDate);

            if (emoteUsageResult == null)
            {
                await ctx.RespondAsync("No stats yet");
                return;
            }

            var emoteStats = emoteUsageResult.EmoteStats;
            var emoteUsers = emoteUsageResult.EmoteUsers;

            var emoteCode = EmoteHelper.BuildEmoteCode(emoteStats.EmoteId, emoteStats.IsAnimated);
            var totalUsage = emoteStats.Usage;

            var title = $"Stats for `{emoteCode}`";

            if (fromDate != null)
            {
                title += $" from {fromDate.Value:yyyy-MM-dd}";
            }

            var result = new StringBuilder();

            result.AppendLine(title);
            result.AppendLine($"Total usage: {totalUsage}");

            if (emoteUsers.Count > 0)
            {
                var extendedEmoteUsers = emoteUsers
                                        .Select(r => new ExtendedEmoteUser()
                                        {
                                            UserId = r.UserId,
                                            Usage = r.Usage,
                                            PercentageOfTotal = (double)r.Usage / totalUsage

                                        })
                                        .ToList();

                // Resolve display names
                foreach (var emoteUser in extendedEmoteUsers)
                {
                    DiscordMember user;

                    if (ctx.Guild.Members.ContainsKey(emoteUser.UserId))
                    {
                        user = ctx.Guild.Members[emoteUser.UserId];
                    }
                    else
                    {
                        try
                        {
                            user = await ctx.Guild.GetMemberAsync(emoteUser.UserId);
                        }
                        catch
                        {
                            user = null!;
                        }
                    }

                    if (user != null)
                    {
                        emoteUser.DisplayName = user.GetNicknameOrUsername();
                    }
                    else
                    {
                        emoteUser.DisplayName = "Unknown user";
                    }
                }

                result.AppendLine();
                result.AppendLine("Top users");

                var lines = FormattedResultHelper.FormatExtendedEmoteUsers(extendedEmoteUsers, 0);

                lines.ForEach(l => result.AppendLine(l));
            }

            var formattedResultMessage = $"`{result}`";

            await ctx.RespondAsync(formattedResultMessage);
        }


        [Command("help")]
        [Description("Help about stats")]
        public Task GetHelpStats(CommandContext ctx)
        {
            var sb = new StringBuilder();

            var commandPrefix = _options.CommandPrefix;

            sb.AppendLine();
            sb.AppendLine($"Command arguments:");
            sb.AppendLine($"`username       .. Discord username with # identifier or @mention`");
            sb.AppendLine($"`emote          .. Discord emote (server emotes only)`");
            sb.AppendLine($"`-p, --page     .. Displays a different page of the result set (default 1st page)`");
            sb.AppendLine($"`-i, --interval .. Limits result set to given interval (default 2 months)`");
            sb.AppendLine($"`                    Valid interval units: \"m\", \"w\", \"d\"`");
            sb.AppendLine($"`                    Optionally use interval value \"lifetime\"`");
            sb.AppendLine();
            sb.AppendLine($"Usage examples:");
            sb.AppendLine($"`{commandPrefix}stats best`");
            sb.AppendLine($"`{commandPrefix}stats worst --page 2`");
            sb.AppendLine($"`{commandPrefix}stats user User#1234 --interval 3m`");
            sb.AppendLine($"`{commandPrefix}stats me -p 2 -i 2w`");


            var result = FormattedResultHelper.BuildMessage($"Shows server-wide emote stats-or for a specific user", sb.ToString());

            return ctx.RespondAsync(result);
        }

        private DateTime? GetDateFromInterval(IntervalValue interval)
        {
            if (interval.IsLifetime)
                return null;

            var date = DateTime.UtcNow;

            var unit = interval.Unit;
            var value = interval.NumericValue;

            switch (unit)
            {
                case "m":
                    date = date.AddMonths(-value);
                    break;
                case "w":
                    date = date.AddDays(-value * 7);
                    break;
                case "d":
                    date = date.AddDays(-value);
                    break;
            }

            return date;
        }

        /// <summary>
        /// Check if emote belongs to guild
        /// TODO: Refactor (duplicated in EmoteMessageService)
        /// </summary>
        /// <returns></returns>
        private bool IsValidMessageEmote(ulong emoteId, DiscordGuild guild)
        {
            return guild.Emojis.ContainsKey(emoteId);
        }
    }
}
