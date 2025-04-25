using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Common.Utils;
using Kattbot.Helpers;
using Kattbot.Services.GptImages;
using Kattbot.Services.Images;
using MediatR;
using SixLabors.ImageSharp;
using CreateImageRequest = Kattbot.Services.GptImages.CreateImageRequest;
using CreateImageResponse = Kattbot.Services.GptImages.CreateImageResponse;

namespace Kattbot.CommandHandlers.Images;

#pragma warning disable SA1402 // File may only contain a single type
public class GptImageCommand : CommandRequest
{
    public GptImageCommand(CommandContext ctx, string prompt)
        : base(ctx)
    {
        Prompt = prompt;
    }

    public string Prompt { get; }
}

public class GptImageHandler : IRequestHandler<GptImageCommand>
{
    private const string CreateImageModel = "gpt-image-1";
    private const string Moderation = "low";
    private const string Quality = "medium";

    private readonly GptImagesHttpClient _gptImagesHttpClient;
    private readonly ImageService _imageService;

    public GptImageHandler(GptImagesHttpClient gptImagesHttpClient, ImageService imageService)
    {
        _gptImagesHttpClient = gptImagesHttpClient;
        _imageService = imageService;
    }

    public async Task Handle(GptImageCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage discordMessage = ctx.Message;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string prompt = discordMessage.SubstituteMentions(request.Prompt);

            var imageRequest = new CreateImageRequest
            {
                Prompt = prompt,
                Model = CreateImageModel,
                Quality = Quality,
                Moderation = Moderation,
                User = request.Ctx.User.Id.ToString(),
            };

            CreateImageResponse response = await _gptImagesHttpClient.CreateImage(imageRequest);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            ImageResponseData imageData = response.Data.First();

            Image image = ImageService.ConvertBase64ToImage(imageData.B64Json);

            ImageStreamResult imageStream = await _imageService.GetImageStream(image);

            string fileName = prompt.ToSafeFilename(imageStream.FileExtension);

            string truncatedPrompt = prompt.Length > DiscordConstants.MaxEmbedTitleLength
                ? $"{prompt[..(DiscordConstants.MaxEmbedTitleLength - 3)]}..."
                : prompt;

            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                .WithTitle(truncatedPrompt)
                .WithImageUrl($"attachment://{fileName}");

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(fileName, imageStream.MemoryStream)
                .AddEmbed(eb)
                .WithContent($"There you go {request.Ctx.Member?.Mention ?? "Unknown user"}");

            await request.Ctx.RespondAsync(mb);
        }
        finally
        {
            await message.DeleteAsync();
        }
    }
}
