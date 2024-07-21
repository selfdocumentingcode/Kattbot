using System.Threading.Tasks;
using DSharpPlus.Clients;
using DSharpPlus.Net.Gateway;

namespace Kattbot.Infrastructure;

// ReSharper disable once ClassNeverInstantiated.Global
public class NoWayGateway : IGatewayController
{
    public ValueTask ZombiedAsync(IGatewayClient client)
    {
        // TODO implement zombie handling
        return ValueTask.CompletedTask;
    }

    public Task HeartbeatedAsync(IGatewayClient client)
    {
        return Task.CompletedTask;
    }
}
