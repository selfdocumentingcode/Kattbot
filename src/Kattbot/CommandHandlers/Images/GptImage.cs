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
using Microsoft.Extensions.Logging;
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

public class GptImageAvatarCommand : CommandRequest
{
    public GptImageAvatarCommand(CommandContext ctx, DiscordUser user, string prompt)
        : base(ctx)
    {
        Prompt = prompt;
        User = user;
    }

    public string Prompt { get; }

    public DiscordUser User { get; }
}

public class GptImageEmoteCommand : CommandRequest
{
    public GptImageEmoteCommand(CommandContext ctx, DiscordEmoji emoji, string prompt)
        : base(ctx)
    {
        Prompt = prompt;
        Emoji = emoji;
    }

    public string Prompt { get; }

    public DiscordEmoji Emoji { get; }
}

public class GptImageHandler : IRequestHandler<GptImageCommand>,
    IRequestHandler<GptImageAvatarCommand>,
    IRequestHandler<GptImageEmoteCommand>
{
    private const string GptImageModel = "gpt-image-1";
    private const string Moderation = "low";
    private const string Quality = "medium";

    private const int MaxImageSizeInMb = 25;

    private readonly GptImagesHttpClient _gptImagesHttpClient;
    private readonly ImageService _imageService;
    private readonly DiscordResolver _discordResolver;
    private readonly ILogger<GptImageHandler> _logger;

    private readonly string[] _supportedImageFormats = ["png", "jpg", "webp"];

    public GptImageHandler(
        GptImagesHttpClient gptImagesHttpClient,
        ImageService imageService,
        DiscordResolver discordResolver,
        ILogger<GptImageHandler> logger)
    {
        _gptImagesHttpClient = gptImagesHttpClient;
        _imageService = imageService;
        _discordResolver = discordResolver;
        _logger = logger;
    }

    public async Task Handle(GptImageCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage discordMessage = ctx.Message;

        DiscordMessage ackMessage = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string prompt = discordMessage.SubstituteMentions(request.Prompt);

            string? imageUrl = await discordMessage.GetImageUrlFromMessage(_logger);

            CreateImageResponse response;
            var userId = request.Ctx.User.Id.ToString();

            if (imageUrl == null)
            {
                var imageRequest = new CreateImageRequest
                {
                    Prompt = prompt,
                    Model = GptImageModel,
                    Quality = Quality,
                    Moderation = Moderation,
                    User = userId,
                };

                response = await _gptImagesHttpClient.CreateImage(imageRequest, cancellationToken);
            }
            else
            {
                response = await CreateImageEdit(imageUrl, prompt, userId, cancellationToken);
            }

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            ImageResponseData imageData = response.Data.First();

            Image image = ImageService.ConvertBase64ToImage(imageData.B64Json);

            ImageStreamResult imageStream = await ImageService.GetImageStream(image);

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
            await ackMessage.DeleteAsync();
        }
    }

    public async Task Handle(GptImageAvatarCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage discordMessage = ctx.Message;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMessage ackMessage = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string prompt = discordMessage.SubstituteMentions(request.Prompt);

            DiscordMember userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id) ??
                                         throw new Exception("Invalid user");

            string imageUrl = userAsMember.GuildAvatarUrl ?? userAsMember.AvatarUrl;

            var userId = request.Ctx.User.Id.ToString();

            CreateImageResponse response = await CreateImageEdit(imageUrl, prompt, userId, cancellationToken);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            ImageResponseData imageData = response.Data.First();

            Image image = ImageService.ConvertBase64ToImage(imageData.B64Json);

            ImageStreamResult imageStream = await ImageService.GetImageStream(image);

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
            await ackMessage.DeleteAsync();
        }
    }

    public async Task Handle(GptImageEmoteCommand request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage discordMessage = ctx.Message;

        DiscordEmoji emoji = request.Emoji;

        DiscordMessage ackMessage = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string prompt = discordMessage.SubstituteMentions(request.Prompt);

            string imageUrl = emoji.GetEmojiImageUrl();

            var userId = request.Ctx.User.Id.ToString();

            CreateImageResponse response = await CreateImageEdit(imageUrl, prompt, userId, cancellationToken);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            ImageResponseData imageData = response.Data.First();

            Image image = ImageService.ConvertBase64ToImage(imageData.B64Json);

            ImageStreamResult imageStream = await ImageService.GetImageStream(image);

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
            await ackMessage.DeleteAsync();
        }
    }

    private async Task<CreateImageResponse> CreateImageEdit(
        string imageUrl,
        string prompt,
        string userId,
        CancellationToken cancellationToken)
    {
        Image editImage = await _imageService.DownloadImage(imageUrl);

        Image imageInSupportedFormat =
            await ImageService.EnsureSupportedImageFormatOrPng(editImage, _supportedImageFormats);

        Image resizedImage = await ImageService.EnsureMaxImageFileSize(
            imageInSupportedFormat,
            MaxImageSizeInMb);

        var tempFileName = $"{Guid.NewGuid()}.png";

        ImageStreamResult inputImageStream = await ImageService.GetImageStream(resizedImage);

        var editImageRequest = new CreateImageEditRequest
        {
            Prompt = prompt,
            Image = inputImageStream.MemoryStream.ToArray(),
            Model = GptImageModel,
            Quality = Quality,
            User = userId,
        };

        CreateImageResponse response = await _gptImagesHttpClient.CreateImageEdit(
            editImageRequest,
            tempFileName,
            cancellationToken);

        return response;
    }
}
