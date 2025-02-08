using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Kattbot.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kattbot.Workers;

public class BotWorker : IHostedService
{
    private readonly DiscordClient _client;
    private readonly ILogger<BotWorker> _logger;
    private readonly BotOptions _options;

    public BotWorker(
        IOptions<BotOptions> options,
        ILogger<BotWorker> logger,
        DiscordClient client)
    {
        _options = options.Value;
        _logger = logger;
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bot");

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
}
