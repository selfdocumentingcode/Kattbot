using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;

namespace Kattbot.CommandHandlers.Images;

public class TransformImageMessageRequest : CommandRequest
{
    public TransformImageMessageRequest(CommandContext ctx, TransformImageEffect effect)
        : base(ctx)
    {
        Effect = effect;
    }

    public TransformImageEffect Effect { get; }
}

public class TransformImageMessageHandler : IRequestHandler<TransformImageMessageRequest>
{
    private readonly ImageService _imageService;

    public TransformImageMessageHandler(ImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task Handle(TransformImageMessageRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage message = ctx.Message;
        TransformImageEffect effect = request.Effect;

        string? imageUrl = await message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        ImageStreamResult imageStreamResult = await _imageService.TransformImage(imageUrl, effect);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var imageFilename = $"{Guid.NewGuid()}.{fileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }
}
