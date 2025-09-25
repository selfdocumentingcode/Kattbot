using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Common.Utils;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kattbot.CommandHandlers.Images;

public class TransformImageEmoteRequest : CommandRequest
{
    public TransformImageEmoteRequest(CommandContext ctx, DiscordEmoji emoji, TransformImageEffect effect)
        : base(ctx)
    {
        Emoji = emoji;
        Effect = effect;
    }

    public DiscordEmoji Emoji { get; }

    public TransformImageEffect Effect { get; }
}

public class TransformImageMessageRequest : CommandRequest
{
    public TransformImageMessageRequest(CommandContext ctx, TransformImageEffect effect)
        : base(ctx)
    {
        Effect = effect;
    }

    public TransformImageEffect Effect { get; }
}

public class TransformImageUserRequest : CommandRequest
{
    public TransformImageUserRequest(CommandContext ctx, DiscordUser user, TransformImageEffect effect)
        : base(ctx)
    {
        User = user;
        Effect = effect;
    }

    public DiscordUser User { get; }

    public TransformImageEffect Effect { get; }
}

public class TransformImageHandler : IRequestHandler<TransformImageEmoteRequest>,
    IRequestHandler<TransformImageMessageRequest>,
    IRequestHandler<TransformImageUserRequest>
{
    private readonly ImageService _imageService;
    private readonly DiscordResolver _discordResolver;

    public TransformImageHandler(ImageService imageService, DiscordResolver discordResolver)
    {
        _imageService = imageService;
        _discordResolver = discordResolver;
    }

    public async Task Handle(TransformImageEmoteRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;
        TransformImageEffect effect = request.Effect;

        string imageUrl = emoji.GetEmojiImageUrl();

        ImageStreamResult imageStreamResult = await TransformImage(imageUrl, effect);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var fileName = $"{Guid.NewGuid()}.{fileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(fileName, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(TransformImageMessageRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage message = ctx.Message;
        TransformImageEffect effect = request.Effect;

        string? imageUrl = await message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        ImageStreamResult imageStreamResult = await TransformImage(imageUrl, effect);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var imageFilename = $"{Guid.NewGuid()}.{fileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(TransformImageUserRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;
        TransformImageEffect effect = request.Effect;

        DiscordMember userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id)
                                     ?? throw new Exception("Invalid user");

        string imageUrl = userAsMember.GuildAvatarUrl
                          ?? userAsMember.AvatarUrl
                          ?? throw new Exception("Couldn't load user avatar");

        ImageStreamResult imageStreamResult =
            await TransformImage(imageUrl, effect, ImageEffects.CropToCircle);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageFilename = userAsMember.DisplayName.ToSafeFilename(fileExtension);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }

    private async Task<ImageStreamResult> TransformImage(
        string imageUrl,
        TransformImageEffect effect,
        ImageTransformDelegate<Rgba32>? preTransform = null)
    {
        Image<Rgba32> inputImage = await _imageService.DownloadImage<Rgba32>(imageUrl);

        if (preTransform != null)
        {
            inputImage = preTransform(inputImage);
        }

        Image imageResult;

        if (effect == TransformImageEffect.DeepFry)
        {
            imageResult = ImageEffects.DeepFryImage(inputImage);
        }
        else if (effect == TransformImageEffect.OilPaint)
        {
            imageResult = ImageEffects.OilPaintImage(inputImage);
        }
        else if (effect == TransformImageEffect.Twirl)
        {
            imageResult = ImageEffects.TwirlImage(inputImage, angleDeg: 90);
        }
        else
        {
            throw new InvalidOperationException($"Unknown effect: {effect}");
        }

        ImageStreamResult imageStreamResult = await ImageService.GetImageStream(imageResult);

        return imageStreamResult;
    }
}

public enum TransformImageEffect
{
    DeepFry,
    OilPaint,
    Twirl,
}
