using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using MediatR;

namespace Kattbot.CommandHandlers.Images;

public class DallifyImageEmoteRequest : CommandRequest
{
    public DallifyImageEmoteRequest(CommandContext ctx, DiscordEmoji emoji)
        : base(ctx)
    {
        Emoji = emoji;
    }

    public DiscordEmoji Emoji { get; }
}

public class DallifyImageEmoteHandler : DallifyImageHandlerBase,
    IRequestHandler<DallifyImageEmoteRequest>
{
    public DallifyImageEmoteHandler(
        DalleHttpClient dalleHttpClient,
        ImageService imageService)
        : base(dalleHttpClient, imageService)
    { }

    public async Task Handle(DallifyImageEmoteRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        ulong userId = ctx.User.Id;

        DiscordEmoji emoji = request.Emoji;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string imageUrl = emoji.GetEmojiImageUrl();

            ImageStreamResult imageStreamResult = await DallifyImage(imageUrl, userId, Size256);

            using MemoryStream imageStream = imageStreamResult.MemoryStream;
            string fileExtension = imageStreamResult.FileExtension;

            string imageName = emoji.Id != 0 ? emoji.Id.ToString() : emoji.Name;

            var fileName = $"{imageName}.{fileExtension}";

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(fileName, imageStream)
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
