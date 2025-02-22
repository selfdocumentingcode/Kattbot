using System.Threading.Tasks;
using DSharpPlus.Net.Gateway;

namespace Kattbot.Infrastructure;

// ReSharper disable once ClassNeverInstantiated.Global
public class NoWayGateway : IGatewayController
{
    public async Task ZombiedAsync(IGatewayClient client)
    {
        await client.ReconnectAsync();
    }

    public Task HeartbeatedAsync(IGatewayClient client)
    {
        return Task.CompletedTask;
    }

    public Task ResumeAttemptedAsync(IGatewayClient client)
    {
        return Task.CompletedTask;
    }

    public Task ReconnectRequestedAsync(IGatewayClient client)
    {
        return Task.CompletedTask;
    }

    public Task ReconnectFailedAsync(IGatewayClient client)
    {
        return Task.CompletedTask;
    }

    public Task SessionInvalidatedAsync(IGatewayClient client)
    {
        return Task.CompletedTask;
    }
}
