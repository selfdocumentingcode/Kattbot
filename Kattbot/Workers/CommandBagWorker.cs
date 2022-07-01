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
    public class CommandBag : ConcurrentBag<CommandRequest>
    {

    }

    public class CommandBagWorker : BackgroundService
    {
        private const int IdleDelay = 1000;
        private const int BusyDelay = 10;

        private readonly ILogger<CommandBagWorker> _logger;
        private readonly CommandBag _commandBag;
        private readonly IMediator _mediator;

        public CommandBagWorker(
                ILogger<CommandBagWorker> logger,
                CommandBag commandBag,
                IMediator mediator
            )
        {
            _logger = logger;
            _commandBag = commandBag;
            _mediator = mediator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var nextDelay = IdleDelay;

                try
                {
                    if (_commandBag.Count == 0)
                    {
                        await Task.Delay(nextDelay, stoppingToken);

                        continue;
                    }

                    _commandBag.TryTake(out var command);

                    if (command != null)
                    {
                        _logger.LogDebug($"Grabbed command. {_commandBag.Count} left in the bag");

                        _ = Task.Run(() => _mediator.Send(command));

                        nextDelay = BusyDelay;
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is TaskCanceledException))
                    {
                        _logger.LogError(ex, typeof(CommandQueueWorker).Name);
                    }
                }

                await Task.Delay(nextDelay, stoppingToken);
            }
        }
    }
}
