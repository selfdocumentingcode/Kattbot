using System.IO;
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
    public GetBigEmoteRequest(CommandContext ctx, DiscordEmoji emoji, uint? scaleFactor = null)
    : base(ctx)
    {
        Emoji = emoji;
        ScaleFactor = scaleFactor;
    }

    public DiscordEmoji Emoji { get; set; }

    public uint? ScaleFactor { get; set; }
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

        string url = emoji.GetEmojiImageUrl();

        using var image = await _imageService.DownloadImage(url);

        var imageStreamResult = hasScaleFactor
                ? await _imageService.ScaleImage(image, request.ScaleFactor!.Value)
                : await _imageService.GetImageStream(image);

        MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var responseBuilder = new DiscordMessageBuilder();

        string fileName = hasScaleFactor ? $"big_x{request.ScaleFactor}" : "big";

        responseBuilder.AddFile($"{fileName}.{fileExtension}", imageStream);

        await ctx.RespondAsync(responseBuilder);
    }
}
