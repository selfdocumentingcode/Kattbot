using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandModules.ResultFormatters;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.Common.Models;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Helpers;
using Kattbot.Workers;
using Microsoft.Extensions.Options;
using static Kattbot.CommandHandlers.EmoteStats.GetEmoteStats;
using static Kattbot.CommandHandlers.EmoteStats.GetGuildEmoteStats;
using static Kattbot.CommandHandlers.EmoteStats.GetUserEmoteStats;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
[Group("stats")]
[ModuleLifespan(ModuleLifespan.Transient)]
public class StatsCommandModule : BaseCommandModule
{
    private readonly BotOptions _options;
    private readonly CommandQueueChannel _commandQueue;

    public StatsCommandModule(
        IOptions<BotOptions> options,
        CommandQueueChannel commandQueue)
    {
        _options = options.Value;
        _commandQueue = commandQueue;
    }

    [Command("best")]
    [Description("Return best emotes")]
    public Task GetBestEmotes(CommandContext ctx, [RemainingText] StatsCommandArgs? args = null)
    {
        args ??= new StatsCommandArgs();

        IReadOnlyDictionary<ulong, DiscordEmoji> guildEmotes = ctx.Guild.Emojis;

        return GetRankedEmotes(ctx, SortDirection.DESC, args.Page, args.Interval, guildEmotes);
    }

    [Command("worst")]
    [Description("Return worst emotes")]
    public Task GetWorstEmotes(CommandContext ctx, [RemainingText] StatsCommandArgs? args = null)
    {
        args ??= new StatsCommandArgs();

        IReadOnlyDictionary<ulong, DiscordEmoji> guildEmotes = ctx.Guild.Emojis;

        return GetRankedEmotes(ctx, SortDirection.ASC, args.Page, args.Interval, guildEmotes);
    }

    [Command("me")]
    [Description("Return best emotes for self")]
    public Task GetBestEmotesSelf(CommandContext ctx, [RemainingText] StatsCommandArgs? args = null)
    {
        args ??= new StatsCommandArgs();

        ulong userId = ctx.User.Id;

        string mention = ctx.User.GetNicknameOrUsername();

        return GetBestEmotesUser(ctx, userId, mention, args.Page, args.Interval);
    }

    [GroupCommand]
    [Description("Return best emotes for user")]
    public async Task GetBestEmotesOtherUser(CommandContext ctx, DiscordUser user, [RemainingText] StatsCommandArgs? args = null)
    {
        args ??= new StatsCommandArgs();

        ulong userId = user.Id;

        string mention = user.GetNicknameOrUsername();

        await GetBestEmotesUser(ctx, userId, mention, args.Page, args.Interval);
    }

    [GroupCommand]
    [Description("Return specific emote stats")]
    public async Task GetEmoteStats(CommandContext ctx, string emoteString, [RemainingText] StatsCommandArgs? args = null)
    {
        args ??= new StatsCommandArgs();

        TempEmote? emoji = EmoteHelper.Parse(emoteString);

        if (emoji != null)
        {
            await GetEmoteStats(ctx, emoji, args.Interval);
        }
        else
        {
            await ctx.RespondAsync("I don't know what to do with this");
        }
    }

    [Command("help")]
    [Description("Help about stats")]
    public Task GetHelpStats(CommandContext ctx)
    {
        var sb = new StringBuilder();

        string commandPrefix = _options.CommandPrefix;

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
        sb.AppendLine($"`{commandPrefix}stats User#1234 --interval 3m`");
        sb.AppendLine($"`{commandPrefix}stats me -p 2 -i 2w`");
        sb.AppendLine($"`{commandPrefix}stats :a_server_emote:`");

        string result = FormattedResultHelper.BuildMessage($"Shows server-wide emote stats-or for a specific user", sb.ToString());

        return ctx.RespondAsync(result);
    }

    private Task GetRankedEmotes(CommandContext ctx, SortDirection direction, int page, string interval, IReadOnlyDictionary<ulong, DiscordEmoji> guildEmotes)
    {
        bool parsed = TryGetDateFromInterval(IntervalValue.Parse(interval), out DateTime? fromDate);

        if (!parsed)
        {
            return ctx.RespondAsync("Invalid interval");
        }

        var request = new GetGuildEmoteStatsRequest(ctx)
        {
            SortDirection = direction,
            Page = page,
            FromDate = fromDate,
            GuildEmojis = guildEmotes,
        };

        return _commandQueue.Writer.WriteAsync(request).AsTask();
    }

    private Task GetBestEmotesUser(CommandContext ctx, ulong userId, string mention, int page, string interval)
    {
        bool parsed = TryGetDateFromInterval(IntervalValue.Parse(interval), out DateTime? fromDate);

        if (!parsed)
        {
            return ctx.RespondAsync("Invalid interval");
        }

        var request = new GetUserEmoteStatsRequest(ctx)
        {
            UserId = userId,
            Mention = mention,
            Page = page,
            FromDate = fromDate,
        };

        return _commandQueue.Writer.WriteAsync(request).AsTask();
    }

    private Task GetEmoteStats(CommandContext ctx, TempEmote emote, string interval)
    {
        DiscordGuild guild = ctx.Guild;

        bool isValidEmote = EmoteHelper.IsValidEmote(emote.Id, guild);

        if (!isValidEmote)
        {
            return ctx.RespondAsync("Invalid emote");
        }

        bool parsed = TryGetDateFromInterval(IntervalValue.Parse(interval), out DateTime? fromDate);

        if (!parsed)
        {
            return ctx.RespondAsync("Invalid interval");
        }

        var request = new GetEmoteStatsRequest(ctx)
        {
            Emote = emote,
            FromDate = fromDate,
        };

        return _commandQueue.Writer.WriteAsync(request).AsTask();
    }

    private bool TryGetDateFromInterval(IntervalValue interval, out DateTime? dateTime)
    {
        if (interval.IsLifetime)
        {
            dateTime = null;
            return true;
        }

        DateTime dateResult = DateTime.UtcNow;

        string unit = interval.Unit;
        int value = interval.NumericValue;

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
}
