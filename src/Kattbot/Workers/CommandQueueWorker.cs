using System;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.CommandHandlers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kattbot.Workers;

public class CommandQueueWorker : BackgroundService
{
    private readonly CommandQueueChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommandQueueWorker> _logger;

    public CommandQueueWorker(
        ILogger<CommandQueueWorker> logger,
        CommandQueueChannel channel,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _channel = channel;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (CommandRequest command in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (command is null) continue;

                _logger.LogDebug(
                    "Dequeued command {CommandType}. {RemainingMessageCount} left in queue",
                    command.GetType().Name,
                    _channel.Reader.Count);

                _ = Task.Run(
                    async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await mediator.Send(command, stoppingToken);
                    },
                    stoppingToken);
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
