using System;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Config;
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

        var contextMessage = $"**Failed command** `{command}` by `{user}` in `{channelName}`(`{guildName}`)";
        var escapedErrorMessage = $"`{EscapeTicks(errorMessage)}`";

        var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMessage}";

        LogError("Failed command", fullErrorMessage);
    }

    public void LogError(EventContext? ctx, string errorMessage)
    {
        string user = ctx?.User is not null ? ctx.User.GetFullUsername() : "Unknown user";
        string channelName = ctx?.Channel?.Name ?? "Unknown channel";
        string guildName = ctx?.Guild?.Name ?? "Unknown guild";
        string eventName = ctx?.EventName ?? "Unknown event";
        string message = ctx?.Message is not null ? EscapeTicks(ctx.Message.Content) : string.Empty;

        var contextMessage = $"**Failed event** `{eventName}` by `{user}` in `{channelName}`(`{guildName}`)";

        if (!string.IsNullOrWhiteSpace(message))
        {
            contextMessage += $"{Environment.NewLine}Message: `{message}`";
        }

        var escapedErrorMessage = $"`{EscapeTicks(errorMessage)}`";

        var fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMessage}";

        LogError("Failed event", fullErrorMessage);
    }

    public void LogError(Exception ex, string message)
    {
        LogError(message, ex.ToString());
    }

    public void LogError(string errorMessage)
    {
        LogError("Error", errorMessage);
    }

    public void LogError(string error, string errorMessage)
    {
        SendErrorLogChannelEmbed(error, errorMessage, DiscordConstants.ErrorEmbedColor);
    }

    public void LogWarning(string warning, string warningMessage)
    {
        SendErrorLogChannelEmbed(warning, warningMessage, DiscordConstants.WarningEmbedColor);
    }

    private static string EscapeTicks(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : value.Replace('`', '\'');
    }

    private void SendErrorLogChannelEmbed(string title, string message, int color)
    {
        ulong errorLogGuildId = _options.ErrorLogGuildId;
        ulong errorLogChannelId = _options.ErrorLogChannelId;

        DiscordEmbed messageEmbed = EmbedBuilderHelper.BuildSimpleEmbed(title, message, color);

        var discordLogItem = new DiscordLogItem<DiscordEmbed>(messageEmbed, errorLogGuildId, errorLogChannelId);

        _ = _channel.Writer.TryWrite(discordLogItem);
    }
}
