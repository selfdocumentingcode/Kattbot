using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using MediatR;

namespace Kattbot.CommandHandlers.Images;

#pragma warning disable SA1402 // File may only contain a single type
public class DallifyImageCommand : CommandRequest
{
    public DallifyImageCommand(CommandContext ctx, DiscordEmoji emoji)
        : base(ctx)
    {
        Emoji = emoji;
    }

    public DiscordEmoji Emoji { get; set; }
}

public class DallifyImageHandler : IRequestHandler<DallifyImageCommand>
{
    private readonly DalleHttpClient _dalleHttpClient;
    private readonly ImageService _imageService;

    public DallifyImageHandler(DalleHttpClient dalleHttpClient, ImageService imageService)
    {
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
    }

    public async Task Handle(DallifyImageCommand request, CancellationToken cancellationToken)
    {
        DiscordEmoji emoji = request.Emoji;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string url = emoji.GetEmojiImageUrl();

            using var emojiImage = await _imageService.DownloadImage(url);

            var emojiImageAsPng = await ImageService.ConvertImageToPng(emojiImage);

            var squaredEmojiImage = await _imageService.SquareImage(emojiImageAsPng);

            string fileName = $"{Guid.NewGuid()}.png";

            const string size = "256x256";

            var imageVariationRequest = new CreateImageVariationRequest
            {
                Image = squaredEmojiImage.MemoryStream.ToArray(),
                Size = size,
                User = request.Ctx.User.Id.ToString(),
            };

            var response = await _dalleHttpClient.CreateImageVariation(imageVariationRequest, fileName);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            var imageUrl = response.Data.First();

            var image = await _imageService.DownloadImage(imageUrl.Url);

            var imageStream = await _imageService.GetImageStream(image);

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(fileName, imageStream.MemoryStream)
                .WithContent($"There you go {request.Ctx.Member?.Mention ?? "Unknown user"}");

            await message.DeleteAsync();

            await request.Ctx.RespondAsync(mb);
        }
        catch (Exception)
        {
            await message.DeleteAsync();
            throw;
        }
    }
}
