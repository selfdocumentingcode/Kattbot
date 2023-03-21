using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Services.Images;
using Kattbot.Services.KattGpt;
using MediatR;

namespace Kattbot.CommandHandlers.Images;

#pragma warning disable SA1402 // File may only contain a single type
public class DallePromptCommand : CommandRequest
{
    public string Prompt { get; set; }

    public DallePromptCommand(CommandContext ctx, string prompt)
        : base(ctx)
    {
        Prompt = prompt;
    }
}

public class DallePromptCommandHandler : AsyncRequestHandler<DallePromptCommand>
{
    private readonly DalleHttpClient _dalleHttpClient;
    private readonly ImageService _imageService;

    public DallePromptCommandHandler(DalleHttpClient dalleHttpClient, ImageService imageService)
    {
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
    }

    protected override async Task Handle(DallePromptCommand request, CancellationToken cancellationToken)
    {
        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            var response = await _dalleHttpClient.CreateImage(new CreateImageRequest { Prompt = request.Prompt });

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            var imageUrl = response.Data.First();

            var image = await _imageService.LoadImage(imageUrl.Url);

            var imageStream = await _imageService.GetImageStream(image);

            string safeFileName = new(request.Prompt.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            string fileName = $"{safeFileName}.{imageStream.FileExtension}";

            DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
                .WithTitle(request.Prompt)
                .WithImageUrl($"attachment://{fileName}");

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
            .AddFile(fileName, imageStream.MemoryStream)
            .WithEmbed(eb)
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
