using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.NotificationHandlers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers
{
    public class EventQueue : ConcurrentQueue<INotification> { }

    public class EventQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 0;

        private readonly ILogger<EventQueueWorker> _logger;
        private readonly EventQueue _eventQueue;
        private readonly NotificationPublisher _publisher;
        private readonly DiscordErrorLogger _discordErrorLogger;

        public EventQueueWorker(ILogger<EventQueueWorker> logger, EventQueue eventQueue, NotificationPublisher publisher, DiscordErrorLogger discordErrorLogger)
        {
            _logger = logger;
            _eventQueue = eventQueue;
            _publisher = publisher;
            _discordErrorLogger = discordErrorLogger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                INotification? @event = null;

                try
                {
                    if (_eventQueue.Count == 0)
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    if (!_eventQueue.TryDequeue(out @event))
                        continue;

                    _logger.LogDebug($"Dequeued event. {_eventQueue.Count} left in queue");

                    await _publisher.Publish(@event, stoppingToken);

                    nextDelay = BusyDelay;
                }
                catch (AggregateException ex)
                {
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        if (@event != null && @event is EventNotification notification)
                        {
                            await _discordErrorLogger.LogDiscordError(notification.Ctx, innerEx.Message);
                        }

                        _logger.LogError(innerEx, nameof(EventQueueWorker));
                    }
                }
                catch (Exception ex)
                {
                    if (ex is not TaskCanceledException)
                    { 
                        _logger.LogError(ex, nameof(EventQueueWorker));
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
