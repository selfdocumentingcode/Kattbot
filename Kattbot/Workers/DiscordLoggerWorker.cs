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
    private readonly ILogger<DiscordLoggerWorker> _logger;
    private readonly DiscordLogChannel _channel;
    private readonly DiscordClient _client;

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
                if (logItem == null)
                {
                    continue;
                }

                DiscordChannel logChannel = await ResolveLogChannel(logItem.DiscordGuildId, logItem.DiscordChannelId);

                if (logChannel != null)
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

                _logger.LogDebug("Dequeued (parallel) command. {RemainingMessageCount} left in queue", _channel.Reader.Count);
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
