using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;

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

public class TransformImageHandler : IRequestHandler<TransformImageEmoteRequest>,
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
        var effect = request.Effect;

        string url = emoji.GetEmojiImageUrl();

        using var image = await _imageService.DownloadImage(url);

        ImageStreamResult imageStreamResult;

        if (effect == TransformImageEffect.DeepFry)
        {
            imageStreamResult = await _imageService.DeepFryImage(image);
        }
        else if (effect == TransformImageEffect.OilPaint)
        {
            imageStreamResult = await _imageService.OilPaintImage(image);
        }
        else
        {
            throw new InvalidOperationException($"Unknown effect: {effect}");
        }

        MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var responseBuilder = new DiscordMessageBuilder();

        string fileName = $"{Guid.NewGuid()}.png";

        responseBuilder.AddFile($"{fileName}.{fileExtension}", imageStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(TransformImageUserRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;
        var effect = request.Effect;

        DiscordMember? userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id) ?? throw new Exception("Invalid user");

        string avatarUrl = userAsMember.GuildAvatarUrl
                            ?? userAsMember.AvatarUrl
                            ?? throw new Exception("Couldn't load user avatar");

        using var inputImage = await _imageService.DownloadImage(avatarUrl);

        var croppedImage = _imageService.CropImageToCircle(inputImage);

        ImageStreamResult imageStreamResult;

        if (effect == TransformImageEffect.DeepFry)
        {
            imageStreamResult = await _imageService.DeepFryImage(croppedImage);
        }
        else if (effect == TransformImageEffect.OilPaint)
        {
            imageStreamResult = await _imageService.OilPaintImage(croppedImage);
        }
        else
        {
            throw new InvalidOperationException($"Unknown effect: {effect}");
        }

        MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var responseBuilder = new DiscordMessageBuilder();

        string imageFilename = user.GetNicknameOrUsername().ToSafeFilename(fileExtension);

        responseBuilder.AddFile(imageFilename, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }
}
