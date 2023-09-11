﻿using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kattbot.Attributes;
using Kattbot.CommandHandlers.Images;
using Kattbot.Workers;

namespace Kattbot.CommandModules;

[BaseCommandCheck]
public class ImageModule : BaseCommandModule
{
    private readonly CommandQueueChannel _commandParallelQueue;

    public ImageModule(CommandQueueChannel commandParallelQueue)
    {
        _commandParallelQueue = commandParallelQueue;
    }

    [Command("deepfry")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task DeepFryEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new TransformImageEmoteRequest(ctx, emoji, TransformImageEffect.DeepFry);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("deepfry")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task DeepFryEmote(CommandContext ctx, DiscordUser user)
    {
        var request = new TransformImageUserRequest(ctx, user, TransformImageEffect.DeepFry);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("deepfry")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public Task DeepFryEmote(CommandContext ctx, string _ = "")
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        var request = new TransformImageMessageRequest(ctx, TransformImageEffect.DeepFry);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task OilPaintEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new TransformImageEmoteRequest(ctx, emoji, TransformImageEffect.OilPaint);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task OilPaintEmote(CommandContext ctx, DiscordUser user)
    {
        var request = new TransformImageUserRequest(ctx, user, TransformImageEffect.OilPaint);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public Task OilPaintEmote(CommandContext ctx, string _ = "")
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        var request = new TransformImageMessageRequest(ctx, TransformImageEffect.OilPaint);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("pet")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task PetEmote(CommandContext ctx, DiscordEmoji emoji, string? speed = null)
    {
        var request = new PetEmoteRequest(ctx)
        {
            Emoji = emoji,
            Speed = speed,
        };

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("pet")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task PetUser(CommandContext ctx, DiscordUser user, string? speed = null)
    {
        var request = new PetUserRequest(ctx)
        {
            User = user,
            Speed = speed,
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

    [Command("dallify")]
    [Cooldown(5, 60, CooldownBucketType.Global)]
    public Task Dallify(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new DallifyEmoteRequest(ctx, emoji);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("dallify")]
    [Cooldown(5, 60, CooldownBucketType.Global)]
    public Task Dallify(CommandContext ctx, DiscordUser user)
    {
        var request = new DallifyUserRequest(ctx, user);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }
}
