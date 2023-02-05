using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.EventHandlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kattbot.Workers;

public class BotWorker : IHostedService
{
    private readonly BotOptions _options;
    private readonly ILogger<BotWorker> _logger;
    private readonly DiscordClient _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly CommandEventHandler _commandEventHandler;
    private readonly EmoteEventHandler _emoteEventHandler;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client,
        IServiceProvider serviceProvider,
        CommandEventHandler commandEventHandler,
        EmoteEventHandler emoteEventHandler)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _serviceProvider = serviceProvider;
        _commandEventHandler = commandEventHandler;
        _emoteEventHandler = emoteEventHandler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        string commandPrefix = _options.CommandPrefix;
        string altCommandPrefix = _options.AlternateCommandPrefix;

        CommandsNextExtension commands = _client.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = new[] { commandPrefix, altCommandPrefix },
            Services = _serviceProvider,
            EnableDefaultHelp = false,
        });

        await _client.ConnectAsync();

        commands.RegisterConverter(new GenericArgumentConverter<StatsCommandArgs, StatsCommandArgsParser>());
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        _client.SocketOpened += OnClientConnected;
        _client.SocketClosed += OnClientDisconnected;
        _client.Ready += OnClientReady;

        _commandEventHandler.RegisterHandlers(commands);
        _emoteEventHandler.RegisterHandlers();
    }

    private Task OnClientDisconnected(DiscordClient sender, SocketCloseEventArgs e)
    {
        _logger.LogInformation($"Bot disconected {e.CloseMessage}");

        return Task.CompletedTask;
    }

    private Task OnClientConnected(DiscordClient sender, SocketEventArgs e)
    {
        _logger.LogInformation("Bot connected");

        return Task.CompletedTask;
    }

    private async Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
    {
        _logger.LogInformation("Bot ready");

        try
        {
            string commandPrefix = _options.CommandPrefix;

            var activity = new DiscordActivity($"\"{commandPrefix}help\" for help", ActivityType.Playing);

            await _client.UpdateStatusAsync(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnClientReady");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot");

        await _client.DisconnectAsync();
    }
}
