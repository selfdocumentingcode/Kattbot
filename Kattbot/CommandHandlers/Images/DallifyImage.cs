using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using MediatR;
using SixLabors.ImageSharp;

namespace Kattbot.CommandHandlers.Images;

public class DallifyEmoteRequest : CommandRequest
{
    public DallifyEmoteRequest(CommandContext ctx, DiscordEmoji emoji)
        : base(ctx)
    {
        Emoji = emoji;
    }

    public DiscordEmoji Emoji { get; set; }
}

public class DallifyUserRequest : CommandRequest
{
    public DallifyUserRequest(CommandContext ctx, DiscordUser user)
        : base(ctx)
    {
        User = user;
    }

    public DiscordUser User { get; set; }
}

public class DallifyImageRequest : CommandRequest
{
    public DallifyImageRequest(CommandContext ctx)
        : base(ctx)
    { }
}

public class DallifyImageHandler : IRequestHandler<DallifyEmoteRequest>,
    IRequestHandler<DallifyUserRequest>,
    IRequestHandler<DallifyImageRequest>
{
    private const int MaxImageSizeInMb = 4;
    public static readonly int Size256 = 256;
    public static readonly int Size512 = 512;
    public static readonly int Size1024 = 1024;

    private static readonly int[] ValidSizes = { Size256, Size512, Size1024 };

    private readonly DalleHttpClient _dalleHttpClient;
    private readonly DiscordResolver _discordResolver;
    private readonly ImageService _imageService;

    public DallifyImageHandler(
        DalleHttpClient dalleHttpClient,
        ImageService imageService,
        DiscordResolver discordResolver)
    {
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
        _discordResolver = discordResolver;
    }

    public async Task Handle(DallifyEmoteRequest request, CancellationToken cancellationToken)
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

    public async Task Handle(DallifyImageRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = ctx.User;
        DiscordMessage message = ctx.Message;

        string? imageUrl = await message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        DiscordMessage wokingOnItMessage = await request.Ctx.RespondAsync("Working on it");

        try
        {
            ImageStreamResult imageStreamResult = await DallifyImage(imageUrl, user.Id, Size1024);

            using MemoryStream imageStream = imageStreamResult.MemoryStream;
            string fileExtension = imageStreamResult.FileExtension;

            var imageFilename = $"{Guid.NewGuid()}.{fileExtension}";

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(imageFilename, imageStream)
                .WithContent($"There you go {request.Ctx.Member?.Mention ?? "Unknown user"}");

            await wokingOnItMessage.DeleteAsync();

            await request.Ctx.RespondAsync(mb);
        }
        catch (Exception)
        {
            await wokingOnItMessage.DeleteAsync();
            throw;
        }
    }

    public async Task Handle(DallifyUserRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            DiscordMember userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id) ??
                                         throw new Exception("Invalid user");

            string imageUrl = userAsMember.GuildAvatarUrl
                              ?? userAsMember.AvatarUrl
                              ?? throw new Exception("Couldn't load user avatar");

            ImageStreamResult imageStreamResult = await DallifyImage(imageUrl, user.Id, Size512);

            using MemoryStream imageStream = imageStreamResult.MemoryStream;
            string fileExtension = imageStreamResult.FileExtension;

            string imageFilename = userAsMember.DisplayName.ToSafeFilename(fileExtension);

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(imageFilename, imageStream)
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

    private async Task<ImageStreamResult> DallifyImage(string imageUrl, ulong userId, int maxSize)
    {
        Image image = await _imageService.DownloadImage(imageUrl);

        Image imageAsPng = await _imageService.ConvertImageToPng(image, MaxImageSizeInMb);

        Image squaredImage = _imageService.CropToSquare(imageAsPng);

        int resultSize = Math.Min(
            maxSize,
            Math.Max(ValidSizes.Reverse().FirstOrDefault(s => squaredImage.Height >= s), ValidSizes[0]));

        var fileName = $"{Guid.NewGuid()}.png";

        ImageStreamResult inputImageStream = await _imageService.GetImageStream(squaredImage);

        var imageVariationRequest = new CreateImageVariationRequest
        {
            Image = inputImageStream.MemoryStream.ToArray(),
            Size = $"{resultSize}x{resultSize}",
            User = userId.ToString(),
        };

        CreateImageResponse response = await _dalleHttpClient.CreateImageVariation(imageVariationRequest, fileName);

        if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

        ImageResponseUrlData imageResponseUrl = response.Data.First();

        Image imageResult = await _imageService.DownloadImage(imageResponseUrl.Url);

        ImageStreamResult imageStream = await _imageService.GetImageStream(imageResult);

        return imageStream;
    }
}
