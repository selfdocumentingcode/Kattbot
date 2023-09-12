using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;
using SixLabors.ImageSharp;

namespace Kattbot.CommandHandlers.Images;

#pragma warning disable SA1402 // File may only contain a single type
public enum TransformImageEffect
{
    DeepFry,
    OilPaint,
}

public class TransformImageEmoteRequest : CommandRequest
{
    public TransformImageEmoteRequest(CommandContext ctx, DiscordEmoji emoji, TransformImageEffect effect)
    : base(ctx)
    {
        Emoji = emoji;
        Effect = effect;
    }

    public DiscordEmoji Emoji { get; set; }

    public TransformImageEffect Effect { get; set; }
}

public class TransformImageUserRequest : CommandRequest
{
    public TransformImageUserRequest(CommandContext ctx, DiscordUser user, TransformImageEffect effect)
    : base(ctx)
    {
        User = user;
        Effect = effect;
    }

    public DiscordUser User { get; set; }

    public TransformImageEffect Effect { get; set; }
}

public class TransformImageMessageRequest : CommandRequest
{
    public TransformImageMessageRequest(CommandContext ctx, TransformImageEffect effect)
    : base(ctx)
    {
        Effect = effect;
    }

    public TransformImageEffect Effect { get; set; }
}

public class TransformImageHandler : IRequestHandler<TransformImageEmoteRequest>,
                                    IRequestHandler<TransformImageUserRequest>,
                                    IRequestHandler<TransformImageMessageRequest>
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
        var ctx = request.Ctx;
        var emoji = request.Emoji;
        var effect = request.Effect;

        string imageUrl = emoji.GetEmojiImageUrl();

        var imageStreamResult = await TransformImage(imageUrl, effect);

        using var imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string fileName = $"{Guid.NewGuid()}.png";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile($"{fileName}.{fileExtension}", imageStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(TransformImageUserRequest request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var user = request.User;
        var guild = ctx.Guild;
        var effect = request.Effect;

        var userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id)
                            ?? throw new Exception("Invalid user");

        string imageUrl = userAsMember.GuildAvatarUrl
                        ?? userAsMember.AvatarUrl
                        ?? throw new Exception("Couldn't load user avatar");

        var imageStreamResult = await TransformImage(imageUrl, effect, _imageService.CropImageToCircle);

        using var imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageFilename = user.GetNicknameOrUsername().ToSafeFilename(fileExtension);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(TransformImageMessageRequest request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var message = ctx.Message;
        var effect = request.Effect;

        var imageUrl = message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        var imageStreamResult = await TransformImage(imageUrl, effect);

        using var imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageFilename = $"{Guid.NewGuid()}.png";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile($"{imageFilename}.{fileExtension}", imageStream);

        await ctx.RespondAsync(responseBuilder);
    }

    private async Task<ImageStreamResult> TransformImage(string imageUrl, TransformImageEffect effect, Func<Image, Image>? preTransform = null)
    {
        var inputImage = await _imageService.DownloadImage(imageUrl);

        if (preTransform != null)
        {
            inputImage = preTransform(inputImage);
        }

        ImageStreamResult imageStreamResult;

        if (effect == TransformImageEffect.DeepFry)
        {
            imageStreamResult = await _imageService.DeepFryImage(inputImage);
        }
        else if (effect == TransformImageEffect.OilPaint)
        {
            imageStreamResult = await _imageService.OilPaintImage(inputImage);
        }
        else
        {
            throw new InvalidOperationException($"Unknown effect: {effect}");
        }

        return imageStreamResult;
    }
}
