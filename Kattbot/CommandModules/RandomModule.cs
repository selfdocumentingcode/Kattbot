using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Kattbot.Attributes;
using Kattbot.CommandHandlers.Speech;
using Kattbot.Workers;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
public class RandomModule : BaseCommandModule
{
    private readonly CommandQueueChannel _commandParallelQueue;

    public RandomModule(CommandQueueChannel commandParallelQueue)
    {
        _commandParallelQueue = commandParallelQueue;
    }

    [Command("meow")]
    public Task Meow(CommandContext ctx)
    {
        var result = new Random().Next(0, 10);

        if (result == 0)
            return ctx.RespondAsync($"Woof!\r\nOops.. I mean... meow? :grimacing:");

        return ctx.RespondAsync("Meow!");
    }

    [Command("mjau")]
    public Task Mjau(CommandContext ctx)
    {
        var result = new Random().Next(0, 10);

        if (result == 0)
            return ctx.RespondAsync("Voff!\r\nOi.. Fytti katta... mjau? :grimacing:");

        return ctx.RespondAsync("Mjau!");
    }

    [Command("prep")]
    public Task GetRandomPrep(CommandContext ctx, string placeholder)
    {
        return ctx.RespondAsync("I no longer offer this service. Ask the cyan guy");
    }

    [Command("speak")]
    [Cooldown(1, 60, CooldownBucketType.User)]
    public async Task Speak(CommandContext ctx, [RemainingText] string text)
    {
        var request = new SpeakTextRequest(ctx, text);
        await _commandParallelQueue.Writer.WriteAsync(request);
    }
}
