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
        var request = new TransformImageRequest(ctx, emoji, TransformImageRequest.EffectDeepFry);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task OilPaintEmote(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new TransformImageRequest(ctx, emoji, TransformImageRequest.EffectOilPaint);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("pet")]
    [Cooldown(5, 10, CooldownBucketType.Global)]
    public Task PetEmote(CommandContext ctx, DiscordEmoji emoji, string? speed = null)
    {
        var request = new GetAnimatedEmoji(ctx)
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
        var request = new GetAnimatedUserAvatar(ctx)
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
}
