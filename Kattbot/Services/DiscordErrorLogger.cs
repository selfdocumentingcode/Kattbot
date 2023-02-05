using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.NotificationHandlers;
using Microsoft.Extensions.Options;

namespace Kattbot.Services;

public class DiscordErrorLogger
{
    private readonly DiscordClient _client;
    private readonly BotOptions _options;

    public DiscordErrorLogger(DiscordClient client, IOptions<BotOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task LogDiscordError(CommandContext ctx, string errorMessage)
    {
        string user = $"{ctx.User.Username}#{ctx.User.Discriminator}";
        string channelName = ctx.Channel.Name;
        string guildName = ctx.Guild.Name;
        string command = EscapeTicks(ctx.Message.Content);

        string contextMessage = $"**Failed command** `{command}` by `{user}` in `{channelName}`(`{guildName}`)";
        string escapedErrorMesssage = $"`{EscapeTicks(errorMessage)}`";

        string fullErrorMessage = $"{contextMessage}{Environment.NewLine}{escapedErrorMesssage}";

        await LogDiscordError(fullErrorMessage);
    }

    public async Task LogDiscordError(EventContext? ctx, string errorMessage)
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

        await LogDiscordError(fullErrorMessage);
    }

    public async Task LogDiscordError(string error)
    {
        ulong errorLogGuilId = _options.ErrorLogGuildId;
        ulong errorLogChannelId = _options.ErrorLogChannelId;

        DiscordChannel errorLogChannel = await ResolveErrorLogChannel(errorLogGuilId, errorLogChannelId);

        if (errorLogChannel != null)
        {
            await errorLogChannel.SendMessageAsync(error);
        }
    }

    private static string EscapeTicks(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : value.Replace('`', '\'');
    }

    private async Task<DiscordChannel> ResolveErrorLogChannel(ulong guildId, ulong channelId)
    {
        _client.Guilds.TryGetValue(guildId, out DiscordGuild? discordGuild);

        if (discordGuild == null)
        {
            discordGuild = await _client.GetGuildAsync(guildId);
        }

        discordGuild.Channels.TryGetValue(channelId, out DiscordChannel? discordChannel);

        if (discordChannel == null)
        {
            discordChannel = discordGuild.GetChannel(channelId);
        }

        return discordChannel;
    }
}
