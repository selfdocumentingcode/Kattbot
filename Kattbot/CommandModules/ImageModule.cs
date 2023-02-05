﻿using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandHandlers.Images;
using Kattbot.Workers;
using static Kattbot.CommandHandlers.Images.GetBigEmote;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
public class ImageModule : BaseCommandModule
{
    private readonly CommandParallelQueueChannel _commandParallelQueue;

    public ImageModule(CommandParallelQueueChannel commandParallelQueue)
    {
        _commandParallelQueue = commandParallelQueue;
    }

    [Command("big")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task BigEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx)
        {
            Emoji = emoji,
        };

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("bigger")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task BiggerEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx)
        {
            Emoji = emoji,
            ScaleFactor = 2,
        };

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("deepfry")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task DeepFryEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx)
        {
            Emoji = emoji,
            ScaleFactor = 2,
            Effect = EffectDeepFry,
        };

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task OilPaintEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new GetBigEmoteRequest(ctx)
        {
            Emoji = emoji,
            ScaleFactor = 2,
            Effect = EffectOilPaint,
        };

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("dalle")]
    [Cooldown(5, 60, CooldownBucketType.Global)]
    public Task Dalle(CommandContext ctx, [RemainingText] string prompt)
    {
        var request = new DallePromptCommand(ctx, prompt);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }
}
