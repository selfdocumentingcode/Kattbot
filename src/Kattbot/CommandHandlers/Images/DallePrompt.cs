using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Common.Utils;
using Kattbot.Helpers;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using MediatR;
using SixLabors.ImageSharp;

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
                User = request.Ctx.User.Id.ToString(),
            };

            CreateImageResponse response = await _dalleHttpClient.CreateImage(imageRequest);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            ImageResponseUrlData imageUrl = response.Data.First();

            Image image = await _imageService.DownloadImage(imageUrl.Url);

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
