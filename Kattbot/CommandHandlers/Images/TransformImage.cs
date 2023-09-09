using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;

namespace Kattbot.CommandHandlers.Images;

public class TransformImageRequest : CommandRequest
{
    public static readonly string EffectDeepFry = "deepfry";
    public static readonly string EffectOilPaint = "oilpaint";

    public TransformImageRequest(CommandContext ctx, DiscordEmoji emoji, string effect)
    : base(ctx)
    {
        Emoji = emoji;
        Effect = effect;
    }

    public DiscordEmoji Emoji { get; set; }

    public string Effect { get; set; }
}

#pragma warning disable SA1402 // File may only contain a single type
public class TransformImageHandler : IRequestHandler<TransformImageRequest>
{
    private readonly ImageService _imageService;

    public TransformImageHandler(ImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task Handle(TransformImageRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;
        string effect = request.Effect;

        string url = emoji.GetEmojiImageUrl();

        using var image = await _imageService.DownloadImage(url);

        ImageStreamResult imageStreamResult;

        if (effect == TransformImageRequest.EffectDeepFry)
        {
            imageStreamResult = await _imageService.DeepFryImage(image);
        }
        else if (effect == TransformImageRequest.EffectOilPaint)
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

        string fileName = effect;

        responseBuilder.AddFile($"{fileName}.{fileExtension}", imageStream);

        await ctx.RespondAsync(responseBuilder);
    }
}
