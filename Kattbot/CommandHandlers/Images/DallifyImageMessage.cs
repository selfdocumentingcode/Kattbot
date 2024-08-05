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

public class DallifyImageMessageRequest : CommandRequest
{
    public DallifyImageMessageRequest(CommandContext ctx)
        : base(ctx)
    { }
}

public class DallifyImageMessageHandler : DallifyImageHandlerBase,
    IRequestHandler<DallifyImageMessageRequest>
{
    public DallifyImageMessageHandler(
        DalleHttpClient dalleHttpClient,
        ImageService imageService)
        : base(dalleHttpClient, imageService)
    { }

    public async Task Handle(DallifyImageMessageRequest messageRequest, CancellationToken cancellationToken)
    {
        CommandContext ctx = messageRequest.Ctx;
        DiscordUser user = ctx.User;
        DiscordMessage message = ctx.Message;

        string? imageUrl = await message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        DiscordMessage wokingOnItMessage = await messageRequest.Ctx.RespondAsync("Working on it");

        try
        {
            ImageStreamResult imageStreamResult = await DallifyImage(imageUrl, user.Id, Size1024);

            using MemoryStream imageStream = imageStreamResult.MemoryStream;
            string fileExtension = imageStreamResult.FileExtension;

            var imageFilename = $"{Guid.NewGuid()}.{fileExtension}";

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(imageFilename, imageStream)
                .WithContent($"There you go {messageRequest.Ctx.Member?.Mention ?? "Unknown user"}");

            await wokingOnItMessage.DeleteAsync();

            await messageRequest.Ctx.RespondAsync(mb);
        }
        catch (Exception)
        {
            await wokingOnItMessage.DeleteAsync();
            throw;
        }
    }
}
