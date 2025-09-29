﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Kattbot.Attributes;
using Kattbot.CommandHandlers.Random;
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
        int result = new Random().Next(minValue: 0, maxValue: 10);

        if (result == 0)
        {
            return ctx.RespondAsync("https://tenor.com/view/arf-cute-puppy-dog-bark-gif-16685500");
        }

        return ctx.RespondAsync("https://tenor.com/view/7tv-emotes-gif-2384164572110866110");
    }

    [Command("prep")]
    public Task GetRandomPrep(CommandContext ctx, string placeholder)
    {
        return ctx.RespondAsync("I no longer offer this service. Ask the cyan guy");
    }

    [Command("speak")]
    [Cooldown(maxUses: 1, resetAfter: 10, CooldownBucketType.User)]
    public async Task Speak(CommandContext ctx, [RemainingText] string text)
    {
        var request = new SpeakTextRequest(ctx, text);
        await _commandParallelQueue.Writer.WriteAsync(request);
    }

    [Command("clap")]
    public async Task Clap(CommandContext ctx, [RemainingText] string message)
    {
        var request = new ClapTextRequest(ctx, message);
        await _commandParallelQueue.Writer.WriteAsync(request);
    }
}
