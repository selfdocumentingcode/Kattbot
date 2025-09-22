using System;
using System.Threading.Tasks;
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
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task DeepFry(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new TransformImageEmoteRequest(ctx, emoji, TransformImageEffect.DeepFry);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("deepfry")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task DeepFry(CommandContext ctx, DiscordUser user)
    {
        var request = new TransformImageUserRequest(ctx, user, TransformImageEffect.DeepFry);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("deepfry")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public Task DeepFry(CommandContext ctx, string _ = "")
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        var request = new TransformImageMessageRequest(ctx, TransformImageEffect.DeepFry);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task OilPaint(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new TransformImageEmoteRequest(ctx, emoji, TransformImageEffect.OilPaint);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task OilPaint(CommandContext ctx, DiscordUser user)
    {
        var request = new TransformImageUserRequest(ctx, user, TransformImageEffect.OilPaint);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("oilpaint")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public Task OilPaint(CommandContext ctx, string _ = "")
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        var request = new TransformImageMessageRequest(ctx, TransformImageEffect.OilPaint);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("twirl")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task Twirl(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new TransformImageEmoteRequest(ctx, emoji, TransformImageEffect.Twirl);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("twirl")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task Twirl(CommandContext ctx, DiscordUser user)
    {
        var request = new TransformImageUserRequest(ctx, user, TransformImageEffect.Twirl);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("twirl")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public Task Twirl(CommandContext ctx, string _ = "")
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
    {
        var request = new TransformImageMessageRequest(ctx, TransformImageEffect.Twirl);
        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("pet")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task Pet(CommandContext ctx, DiscordEmoji emoji, string? speed = null)
    {
        var request = new PetEmoteRequest(ctx, emoji, speed);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("pet")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task Pet(CommandContext ctx, DiscordUser user, string? speed = null)
    {
        var request = new PetUserRequest(ctx, user, speed);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("pet")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task Pet(CommandContext ctx, string? speed = null)
    {
        var request = new PetImageRequest(ctx, speed);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("dumptruck")]
    [Cooldown(maxUses: 5, resetAfter: 10, CooldownBucketType.Global)]
    public Task DumpTruck(CommandContext ctx, DiscordEmoji emoji)
    {
        var request = new DumpTruckEmoteRequest(ctx, emoji);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("gpt-image")]
    [Cooldown(maxUses: 5, resetAfter: 30, CooldownBucketType.Global)]
    public Task GptImage(CommandContext ctx, [RemainingText] string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var request = new GptImageCommand(ctx, prompt);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("gpt-image-avatar")]
    [Cooldown(maxUses: 5, resetAfter: 30, CooldownBucketType.Global)]
    public Task GptImageAvatar(CommandContext ctx, DiscordUser user, [RemainingText] string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var request = new GptImageAvatarCommand(ctx, user, prompt);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }

    [Command("gpt-image-emote")]
    [Cooldown(maxUses: 5, resetAfter: 30, CooldownBucketType.Global)]
    public Task GptImageEmote(CommandContext ctx, DiscordEmoji emoji, [RemainingText] string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

        var request = new GptImageEmoteCommand(ctx, emoji, prompt);

        return _commandParallelQueue.Writer.WriteAsync(request).AsTask();
    }
}
