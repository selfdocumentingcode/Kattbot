using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandHandlers.Images;
using Kattbot.Workers;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
public class EmoteModule : BaseCommandModule
{
    private readonly CommandQueueChannel _commandParallelQueue;

    public EmoteModule(CommandQueueChannel commandParallelQueue)
    {
        _commandParallelQueue = commandParallelQueue;
    }

    [Command("big")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task BigEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx, emoji);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("bigger")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task BiggerEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx, emoji, 2);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("gigantic")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task GiganticEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx, emoji, 3);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("humongous")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task HumongousEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx, emoji, 4);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }
}
