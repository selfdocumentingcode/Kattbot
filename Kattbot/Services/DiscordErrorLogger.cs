﻿using System;
using DSharpPlus.CommandsNext;
using Kattbot.NotificationHandlers;
using Kattbot.Workers;
using Microsoft.Extensions.Options;

namespace Kattbot.Services;

public class DiscordErrorLogger
{
    private readonly DiscordLogChannel _channel;
    private readonly BotOptions _options;

    public DiscordErrorLogger(IOptions<BotOptions> options, DiscordLogChannel channel)
    {
        _channel = channel;
        _options = options.Value;
    }

    public void LogDiscordError(CommandContext ctx, string errorMessage)
    {
        string user = $"{ctx.User.Username}#{ctx.User.Discriminator}";
        string channelName = ctx.Channel.Name;
        string guildName = ctx.Guild.Name;
        string command = EscapeTicks(ctx.Message.Content);

        string contextMessage = $"**Failed command** `{command}` by `{user}` in `{channelName}`(`{guildName}`)";
        string escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

        string fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

        LogDiscordError(fullErrorMessage);
    }

    public void LogDiscordError(EventContext? ctx, string errorMessage)
    {
        string user = ctx?.User != null ? $"{ctx.User.Username}#{ctx.User.Discriminator}" : "Unknown user";
        string channelName = ctx?.Channel?.Name ?? "Unknown channel";
        string guildName = ctx?.Guild?.Name ?? "Unknown guild";
        string eventName = ctx?.EventName ?? "Unknown event";
        string message = ctx?.Message != null ? EscapeTicks(ctx.Message.Content) : string.Empty;

        string contextMessage = $"**Failed event** `{eventName}` by `{user}` in `{channelName}`(`{guildName}`)";

        if (!string.IsNullOrWhiteSpace(message))
        {
            contextMessage += $"{Environment.NewLine}Message: `{message}`";
        }

        string escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

        string fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

        LogDiscordError(fullErrorMessage);
    }

    public void LogDiscordError(string error)
    {
        ulong errorLogGuilId = _options.ErrorLogGuildId;
        ulong errorLogChannelId = _options.ErrorLogChannelId;

        var discordLogItem = new DiscordLogItem(error, errorLogGuilId, errorLogChannelId);

        _ = _channel.Writer.TryWrite(discordLogItem);
    }

    private static string EscapeTicks(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : value.Replace('`', '\'');
    }
}
