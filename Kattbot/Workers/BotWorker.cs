using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.Config;
using Kattbot.EventHandlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kattbot.Workers;

public class BotWorker : IHostedService
{
    private readonly DiscordClient _client;
    private readonly CommandEventHandler _commandEventHandler;
    private readonly ILogger<BotWorker> _logger;
    private readonly BotOptions _options;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client,
        CommandEventHandler commandEventHandler)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _commandEventHandler = commandEventHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        RegisterCommands();

        await ConnectToGateway();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot");

        return _client.DisconnectAsync();
    }

    private async Task ConnectToGateway()
    {
        string commandPrefix = _options.CommandPrefix;

        var activity = new DiscordActivity($"\"{commandPrefix}help\" for help", DiscordActivityType.Playing);

        await _client.ConnectAsync(activity);
    }

    private void RegisterCommands()
    {
        string[] commandPrefixes = { _options.CommandPrefix, _options.AlternateCommandPrefix };

        CommandsNextExtension commands = _client.UseCommandsNext(
            new CommandsNextConfiguration
            {
                StringPrefixes = commandPrefixes,
                EnableDefaultHelp = false,
                EnableMentionPrefix = false,
            });

        commands.RegisterConverter(new GenericArgumentConverter<StatsCommandArgs, StatsCommandArgsParser>());
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        _commandEventHandler.RegisterHandlers(commands);
    }
}
