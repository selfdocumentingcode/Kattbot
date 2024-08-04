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

public class TransformImageEmoteRequest : CommandRequest
{
    public TransformImageEmoteRequest(CommandContext ctx, DiscordEmoji emoji, TransformImageEffect effect)
        : base(ctx)
    {
        Emoji = emoji;
        Effect = effect;
    }

    public DiscordEmoji Emoji { get; }

    public TransformImageEffect Effect { get; }
}

public class TransformImageEmoteHandler : IRequestHandler<TransformImageEmoteRequest>
{
    private readonly ImageService _imageService;

    public TransformImageEmoteHandler(ImageService imageService)
    {
        _imageService = imageService;
    }

    public async Task Handle(TransformImageEmoteRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;
        TransformImageEffect effect = request.Effect;

        string imageUrl = emoji.GetEmojiImageUrl();

        ImageStreamResult imageStreamResult = await _imageService.TransformImage(imageUrl, effect);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var fileName = $"{Guid.NewGuid()}.{fileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(fileName, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }
}
