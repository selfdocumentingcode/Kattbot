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
        var ctx = request.Ctx;
        var discordMessage = ctx.Message;

        var message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            var prompt = discordMessage.SubstituteMentions(request.Prompt);

            var imageRequest = new CreateImageRequest
            {
                Prompt = prompt,
                Model = CreateImageModel,
                User = request.Ctx.User.Id.ToString(),
            };

            var response = await _dalleHttpClient.CreateImage(imageRequest);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            var imageUrl = response.Data.First();

            var image = await _imageService.DownloadImage(imageUrl.Url);

            var imageStream = await _imageService.GetImageStream(image);

            var fileName = prompt.ToSafeFilename(imageStream.FileExtension);

            var truncatedPrompt = prompt.Length > DiscordConstants.MaxEmbedTitleLength
                ? $"{prompt[..(DiscordConstants.MaxEmbedTitleLength - 3)]}..."
                : prompt;

            var eb = new DiscordEmbedBuilder()
                .WithTitle(truncatedPrompt)
                .WithImageUrl($"attachment://{fileName}");

            var mb = new DiscordMessageBuilder()
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
