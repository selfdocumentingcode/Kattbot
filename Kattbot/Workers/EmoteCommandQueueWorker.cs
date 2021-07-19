using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.Data;
using Kattbot.Models;
using Kattbot.Models.Commands;
using Kattbot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot
{
    public class EmoteCommandQueue : ConcurrentQueue<EmoteCommand>
    {

    }

    public class EmoteCommandQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 10;

        private readonly ILogger<EmoteCommandQueueWorker> _logger;
        private readonly EmoteCommandQueue _emoteCommandQueue;
        private readonly IServiceProvider _serviceProvider;

        public EmoteCommandQueueWorker(
                ILogger<EmoteCommandQueueWorker> logger,
                EmoteCommandQueue emoteCommandQueue,
                IServiceProvider serviceProvider
            )
        {
            _logger = logger;
            _emoteCommandQueue = emoteCommandQueue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_emoteCommandQueue.Count == 0)
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _emoteCommandQueue.TryDequeue(out var emoteCommand);

                    if (emoteCommand != null)
                    {
                        _logger.LogDebug($"Dequeued command. {_emoteCommandQueue.Count} left in queue");

                        using var scope = _serviceProvider.CreateScope();

                        var emoteCommandReceiver = scope.ServiceProvider.GetRequiredService<EmoteCommandReceiver>();

                        await emoteCommand.ExecuteAsync(emoteCommandReceiver);

                        nextDelay = BusyDelay;
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, "EmoteCommandQueueWorker");
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
