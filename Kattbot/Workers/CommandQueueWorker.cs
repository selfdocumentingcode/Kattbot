using System;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.CommandHandlers;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers;

public class CommandQueueWorker : BackgroundService
{
    private readonly ILogger<CommandQueueWorker> _logger;
    private readonly CommandQueueChannel _channel;
    private readonly IMediator _mediator;

    public CommandQueueWorker(ILogger<CommandQueueWorker> logger, CommandQueueChannel channel, IMediator mediator)
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
                    _logger.LogDebug("Dequeued command. {RemainingMessageCount} left in queue", _channel.Reader.Count);

                    await _mediator.Send(command, stoppingToken);
                }
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
}
