using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.CommandHandlers;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot
{
    public class CommandQueue : ConcurrentQueue<CommandRequest>
    {

    }

    public class CommandQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 10;

        private readonly ILogger<EmoteCommandQueueWorker> _logger;
        private readonly CommandQueue _commandQueue;
        private readonly IMediator _mediator;

        public CommandQueueWorker(
                ILogger<EmoteCommandQueueWorker> logger,
                CommandQueue emoteCommandQueue,
                IMediator mediator
            )
        {
            _logger = logger;
            _commandQueue = emoteCommandQueue;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_commandQueue.Count == 0)
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _commandQueue.TryDequeue(out var command);

                    if (command != null)
                    {
                        _logger.LogDebug($"Dequeued command. {_commandQueue.Count} left in queue");

                        await _mediator.Send(command);

                        nextDelay = BusyDelay;
                    }
                }
                catch (Exception ex) when (!(ex is TaskCanceledException))
                {
                    _logger.LogError(ex, "CommandQueueWorker");
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
