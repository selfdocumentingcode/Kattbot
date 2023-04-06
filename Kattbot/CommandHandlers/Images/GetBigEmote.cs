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

public class GetBigEmoteRequest : CommandRequest
{
    public static readonly string EffectDeepFry = "deepfry";
    public static readonly string EffectOilPaint = "oilpaint";

    public GetBigEmoteRequest(CommandContext ctx)
    : base(ctx)
    {
    }

    public DiscordEmoji Emoji { get; set; } = null!;

    public uint? ScaleFactor { get; set; }

    public string? Effect { get; set; }
}

#pragma warning disable SA1402 // File may only contain a single type
public class GetBigEmoteHandler : IRequestHandler<GetBigEmoteRequest>
{
    private readonly ImageService _imageService;

    public GetBigEmoteHandler(ImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task Handle(GetBigEmoteRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;
        bool hasScaleFactor = request.ScaleFactor.HasValue;
        string? effect = request.Effect;

        string url = emoji.GetEmojiImageUrl();

        using ImageResult imageResult = await _imageService.LoadImage(url);

        ImageStreamResult imageStreamResult;

        if (effect == GetBigEmoteRequest.EffectDeepFry)
        {
            uint scaleFactor = request.ScaleFactor.HasValue ? request.ScaleFactor.Value : 2;
            imageStreamResult = await _imageService.DeepFryImage(imageResult, scaleFactor);
        }
        else if (effect == GetBigEmoteRequest.EffectOilPaint)
        {
            uint scaleFactor = request.ScaleFactor.HasValue ? request.ScaleFactor.Value : 2;
            imageStreamResult = await _imageService.OilPaintImage(imageResult, scaleFactor);
        }
        else
        {
            imageStreamResult = hasScaleFactor
                ? await _imageService.ScaleImage(imageResult, request.ScaleFactor!.Value)
                : await _imageService.GetImageStream(imageResult);
        }

        MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var responseBuilder = new DiscordMessageBuilder();

        string fileName = hasScaleFactor ? "bigger" : "big";

        responseBuilder.AddFile($"{fileName}.{fileExtension}", imageStream);

        await ctx.RespondAsync(responseBuilder);
    }
}
