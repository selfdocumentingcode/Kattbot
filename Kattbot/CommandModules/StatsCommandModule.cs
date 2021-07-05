using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.Common.Models;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Helper;
using Kattbot.Helpers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Kattbot.CommandHandlers.EmoteStats.GetEmoteStats;
using static Kattbot.CommandHandlers.EmoteStats.GetGuildEmoteStats;
using static Kattbot.CommandHandlers.EmoteStats.GetUserEmoteStats;

namespace Kattbot.CommandModules
{
    [BaseCommandCheck]
    [Group("stats")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class StatsCommandModule : BaseCommandModule
    {
        private readonly BotOptions _options;
        private readonly CommandQueue _commandQueue;

        public StatsCommandModule(
            IOptions<BotOptions> options,
            CommandQueue commandQueue)
        {
            _options = options.Value;
            _commandQueue = commandQueue;
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
            var parsed = TryGetDateFromInterval(IntervalValue.Parse(interval), out var fromDate);

            if (!parsed)
            {
                await ctx.RespondAsync("Invalid interval");
                return;
            }

            var request = new GetGuildEmoteStatsRequest(ctx)
            {
                SortDirection = direction,
                Page = page,
                FromDate = fromDate,
                GuildEmojis = guildEmotes
            };

            _commandQueue.Enqueue(request);
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
            var parsed = TryGetDateFromInterval(IntervalValue.Parse(interval), out var fromDate);

            if (!parsed)
            {
                await ctx.RespondAsync("Invalid interval");
                return;
            }

            var request = new GetUserEmoteStatsRequest(ctx)
            {
                UserId = userId,
                Mention = mention,
                Page = page,
                FromDate = fromDate,
            };

            _commandQueue.Enqueue(request);
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

            var isValidEmote = IsValidMessageEmote(emote.Id, guild);

            if (!isValidEmote)
            {
                await ctx.RespondAsync("Invalid emote");
                return;
            }

            var parsed = TryGetDateFromInterval(IntervalValue.Parse(interval), out var fromDate);

            if(!parsed)
            {
                await ctx.RespondAsync("Invalid interval");
                return;
            }

            var request = new GetEmoteStatsRequest(ctx)
            {
                Emote = emote,
                FromDate = fromDate
            };

            _commandQueue.Enqueue(request);
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
            sb.AppendLine($"`{commandPrefix}stats emote :a_server_emote:`");


            var result = FormattedResultHelper.BuildMessage($"Shows server-wide emote stats-or for a specific user", sb.ToString());

            return ctx.RespondAsync(result);
        }

        private bool TryGetDateFromInterval(IntervalValue interval, out DateTime? dateTime)
        {
            if (interval.IsLifetime)
            {
                dateTime = null;
                return true;
            }               

            var dateResult = DateTime.UtcNow;

            var unit = interval.Unit;
            var value = interval.NumericValue;

            switch (unit)
            {
                case "m":
                    dateResult = dateResult.AddMonths(-value);
                    break;
                case "w":
                    dateResult = dateResult.AddDays(-value * 7);
                    break;
                case "d":
                    dateResult = dateResult.AddDays(-value);
                    break;
            }

            dateTime = dateResult;

            return true;
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
