using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.CommandHandlers;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers
{
    public class CommandParallelQueue : ConcurrentQueue<CommandRequest> { }

    public class CommandParallelQueueWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 10;

        private readonly ILogger<CommandQueueWorker> _logger;
        private readonly CommandParallelQueue _commandQueue;
        private readonly IMediator _mediator;

        public CommandParallelQueueWorker(ILogger<CommandQueueWorker> logger, CommandParallelQueue commandQueue, IMediator mediator)
        {
            _logger = logger;
            _commandQueue = commandQueue;
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
                        _logger.LogDebug($"Dequeued (parallel) command. {_commandQueue.Count} left in queue");

                        _ = Task.Run(() => _mediator.Send(command));

                        nextDelay = BusyDelay;
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, typeof(CommandParallelQueueWorker).Name);
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
