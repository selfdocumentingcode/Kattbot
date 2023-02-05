using System;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.CommandHandlers;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers;

public class CommandParallelQueueWorker : BackgroundService
{
    private readonly ILogger<CommandParallelQueueWorker> _logger;
    private readonly CommandParallelQueueChannel _channel;
    private readonly IMediator _mediator;

    public CommandParallelQueueWorker(ILogger<CommandParallelQueueWorker> logger, CommandParallelQueueChannel channel, IMediator mediator)
    {
        _logger = logger;
        _channel = channel;
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (CommandRequest command in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                if (command != null)
                {
                    _logger.LogDebug("Dequeued (parallel) command. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                    _ = Task.Run(() => _mediator.Send(command, stoppingToken));
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("{Worker} execution is being cancelled", nameof(CommandParallelQueueWorker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Error}", ex.Message);
        }
    }
}
