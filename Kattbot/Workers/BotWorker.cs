using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.CommandModules.TypeReaders;
using Kattbot.EventHandlers;
using Kattbot.NotificationHandlers;
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
    private readonly EventQueueChannel _eventQueue;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client,
        IServiceProvider serviceProvider,
        CommandEventHandler commandEventHandler,
        EmoteEventHandler emoteEventHandler,
        EventQueueChannel eventQueue)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
        _serviceProvider = serviceProvider;
        _commandEventHandler = commandEventHandler;
        _emoteEventHandler = emoteEventHandler;
        _eventQueue = eventQueue;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

        string[] commandPrefixes = new[] { _options.CommandPrefix, _options.AlternateCommandPrefix };

        CommandsNextExtension commands = _client.UseCommandsNext(new CommandsNextConfiguration()
        {
            StringPrefixes = commandPrefixes,
            Services = _serviceProvider,
            EnableDefaultHelp = false,
            EnableMentionPrefix = false,
        });

        commands.RegisterConverter(new GenericArgumentConverter<StatsCommandArgs, StatsCommandArgsParser>());
        commands.RegisterCommands(Assembly.GetExecutingAssembly());

        _client.SocketOpened += OnClientConnected;
        _client.SocketClosed += OnClientDisconnected;
        _client.SessionCreated += OnClientReady;

        _commandEventHandler.RegisterHandlers(commands);
        _emoteEventHandler.RegisterHandlers();

        _client.MessageCreated += (sender, args) => OnMessageCreated(args, commandPrefixes, cancellationToken);

        await _client.ConnectAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bot");

        await _client.DisconnectAsync();
    }

    private async Task OnMessageCreated(MessageCreateEventArgs args, string[] commandPrefixes, CancellationToken cancellationToken)
    {
        var author = args.Author;

        if (author.IsBot || author.IsSystem.GetValueOrDefault())
        {
            return;
        }

        // Ignore messages from DMs
        if (args.Guild is null)
        {
            return;
        }

        // Ignore message that starts with the bot's command prefix
        if (commandPrefixes.Any(prefix => args.Message.Content.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await _eventQueue.Writer.WriteAsync(new MessageCreatedNotification(args), cancellationToken);
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

    private async Task OnClientReady(DiscordClient sender, SessionReadyEventArgs e)
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
}
