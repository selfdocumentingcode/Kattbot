using System;
using DSharpPlus.CommandsNext;
using Kattbot.Helpers;
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

    public void LogError(CommandContext ctx, string errorMessage)
    {
        string user = ctx.User.GetFullUsername();
        string channelName = !ctx.Channel.IsPrivate ? ctx.Channel.Name : "DM";
        string guildName = ctx.Guild?.Name ?? "Unknown guild";
        string command = EscapeTicks(ctx.Message.Content);

        string contextMessage = $"**Failed command** `{command}` by `{user}` in `{channelName}`(`{guildName}`)";
        string escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

        string fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

        LogError(fullErrorMessage);
    }

    public void LogError(EventContext? ctx, string errorMessage)
    {
        string user = ctx?.User is not null ? ctx.User.GetFullUsername() : "Unknown user";
        string channelName = ctx?.Channel?.Name ?? "Unknown channel";
        string guildName = ctx?.Guild?.Name ?? "Unknown guild";
        string eventName = ctx?.EventName ?? "Unknown event";
        string message = ctx?.Message is not null ? EscapeTicks(ctx.Message.Content) : string.Empty;

        string contextMessage = $"**Failed event** `{eventName}` by `{user}` in `{channelName}`(`{guildName}`)";

        if (!string.IsNullOrWhiteSpace(message))
        {
            contextMessage += $"{Environment.NewLine}Message: `{message}`";
        }

        string escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

        string fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

        LogError(fullErrorMessage);
    }

    public void LogError(string error)
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
