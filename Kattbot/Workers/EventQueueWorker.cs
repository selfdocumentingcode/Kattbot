using System;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.Infrastructure;
using Kattbot.NotificationHandlers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers;

public class EventQueueWorker : BackgroundService
{
    private readonly EventQueueChannel _channel;
    private readonly DiscordErrorLogger _discordErrorLogger;
    private readonly ILogger<EventQueueWorker> _logger;
    private readonly NotificationPublisher _publisher;

    public EventQueueWorker(
        ILogger<EventQueueWorker> logger,
        EventQueueChannel channel,
        NotificationPublisher publisher,
        DiscordErrorLogger discordErrorLogger)
    {
        _logger = logger;
        _channel = channel;
        _publisher = publisher;
        _discordErrorLogger = discordErrorLogger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (INotification notification in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (notification is null)
                {
                    continue;
                }

                _logger.LogDebug(
                    "Dequeued event {EventType}. {RemainingMessageCount} left in queue",
                    notification.GetType().Name,
                    _channel.Reader.Count);

                try
                {
                    await _publisher.Publish(notification, stoppingToken);
                }
                catch (AggregateException ex)
                {
                    foreach (Exception innerEx in ex.InnerExceptions)
                    {
                        if (notification is EventNotification eventNotification)
                        {
                            _discordErrorLogger.LogError(eventNotification.Ctx, innerEx.Message);
                        }

                        _logger.LogError(innerEx, nameof(EventQueueWorker));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, nameof(EventQueueWorker));
                    _discordErrorLogger.LogError(ex.Message);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(EventQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(EventQueueWorker));
            _discordErrorLogger.LogError(ex.Message);
        }
    }
}
