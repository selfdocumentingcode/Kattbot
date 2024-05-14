using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers;

public class DiscordLoggerWorker : BackgroundService
{
    private readonly DiscordLogChannel _channel;
    private readonly DiscordClient _client;
    private readonly ILogger<DiscordLoggerWorker> _logger;

    public DiscordLoggerWorker(ILogger<DiscordLoggerWorker> logger, DiscordLogChannel channel, DiscordClient client)
    {
        _logger = logger;
        _channel = channel;
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (DiscordLogItem logItem in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (logItem is null)
                {
                    continue;
                }

                DiscordChannel logChannel = await ResolveLogChannel(logItem.DiscordGuildId, logItem.DiscordChannelId);

                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (logChannel is not null)
                {
                    try
                    {
                        await logChannel.SendMessageAsync(logItem.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "{Error}", ex.Message);
                    }
                }

                _logger.LogDebug(
                    "Dequeued (parallel) command {CommandType}. {RemainingMessageCount} left in queue",
                    logItem.GetType().Name,
                    _channel.Reader.Count);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(CommandQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }

    private async Task<DiscordChannel> ResolveLogChannel(ulong guildId, ulong channelId)
    {
        _client.Guilds.TryGetValue(guildId, out DiscordGuild? discordGuild);

        discordGuild ??= await _client.GetGuildAsync(guildId);

        discordGuild.Channels.TryGetValue(channelId, out DiscordChannel? discordChannel);

        return discordChannel ?? (await discordGuild.GetChannelAsync(channelId));
    }
}
