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

namespace Kattbot.CommandHandlers.Images;

#pragma warning disable SA1402 // File may only contain a single type
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

public class GptImageAvatarHandler : IRequestHandler<GptImageAvatarCommand>
{
    private const string GptImageModel = "gpt-image-1";
    private const string Quality = "medium";

    private const int MaxImageSizeInMb = 25;
    private readonly DiscordResolver _discordResolver;

    private readonly GptImagesHttpClient _gptImagesHttpClient;
    private readonly ImageService _imageService;

    private readonly string[] _supportedImageFormats = ["png", "jpg", "webp"];

    public GptImageAvatarHandler(
        GptImagesHttpClient gptImagesHttpClient,
        ImageService imageService,
        DiscordResolver discordResolver)
    {
        _gptImagesHttpClient = gptImagesHttpClient;
        _imageService = imageService;
        _discordResolver = discordResolver;
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

            string imageUrl = (string?)userAsMember.GuildAvatarUrl ?? userAsMember.AvatarUrl;

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
                User = request.Ctx.User.Id.ToString(),
            };

            CreateImageResponse response = await _gptImagesHttpClient.CreateImageEdit(
                editImageRequest,
                tempFileName,
                cancellationToken);

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
}
