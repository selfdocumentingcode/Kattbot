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
public class DallePromptCommand : CommandRequest
{
    public DallePromptCommand(CommandContext ctx, string prompt)
        : base(ctx)
    {
        Prompt = prompt;
    }

    public string Prompt { get; set; }
}

public class DallePromptHandler : IRequestHandler<DallePromptCommand>
{
    private const string CreateImageModel = "dall-e-3";

    private readonly DalleHttpClient _dalleHttpClient;
    private readonly ImageService _imageService;

    public DallePromptHandler(DalleHttpClient dalleHttpClient, ImageService imageService)
    {
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
    }

    public async Task Handle(DallePromptCommand request, CancellationToken cancellationToken)
    {
        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            var imageRequest = new CreateImageRequest
            {
                Prompt = request.Prompt,
                Model = CreateImageModel,
                User = request.Ctx.User.Id.ToString(),
            };

            var response = await _dalleHttpClient.CreateImage(imageRequest);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            var imageUrl = response.Data.First();

            var image = await _imageService.DownloadImage(imageUrl.Url);

            var imageStream = await _imageService.GetImageStream(image);

            var fileName = request.Prompt.ToSafeFilename(imageStream.FileExtension);

            var truncatedPrompt = request.Prompt.Length > DiscordConstants.MaxEmbedTitleLength
                ? $"{request.Prompt[..(DiscordConstants.MaxEmbedTitleLength - 3)]}..."
                : request.Prompt;

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
