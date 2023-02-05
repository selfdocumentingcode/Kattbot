using System;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.NotificationHandlers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers;

public class EventQueueWorker : BackgroundService
{
    private readonly ILogger<EventQueueWorker> _logger;
    private readonly EventQueueChannel _channel;
    private readonly NotificationPublisher _publisher;
    private readonly DiscordErrorLogger _discordErrorLogger;

    public EventQueueWorker(ILogger<EventQueueWorker> logger, EventQueueChannel channel, NotificationPublisher publisher, DiscordErrorLogger discordErrorLogger)
    {
        _logger = logger;
        _channel = channel;
        _publisher = publisher;
        _discordErrorLogger = discordErrorLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        INotification? @event = null;

        try
        {
            await foreach (INotification notification in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                @event = notification;

                if (@event != null)
                {
                    _logger.LogDebug("Dequeued event. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                    await _publisher.Publish(@event, stoppingToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(EventQueueWorker));
        }
        catch (AggregateException ex)
        {
            foreach (Exception innerEx in ex.InnerExceptions)
            {
                if (@event is not null and EventNotification notification)
                {
                    _discordErrorLogger.LogDiscordError(notification.Ctx, innerEx.Message);
                }

                _logger.LogError(innerEx, nameof(EventQueueWorker));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(EventQueueWorker));
        }
    }
}
