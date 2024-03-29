﻿using System;
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
    public static readonly int Size256 = 256;
    public static readonly int Size512 = 512;
    public static readonly int Size1024 = 1024;

    private const int MaxImageSizeInMb = 4;

    private static readonly int[] ValidSizes = { Size256, Size512, Size1024 };

    private readonly DalleHttpClient _dalleHttpClient;
    private readonly ImageService _imageService;
    private readonly DiscordResolver _discordResolver;

    public DallifyImageHandler(DalleHttpClient dalleHttpClient, ImageService imageService, DiscordResolver discordResolver)
    {
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
        _discordResolver = discordResolver;
    }

    public async Task Handle(DallifyEmoteRequest request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var userId = ctx.User.Id;

        var emoji = request.Emoji;

        var message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            var imageUrl = emoji.GetEmojiImageUrl();

            var imageStreamResult = await DallifyImage(imageUrl, userId, Size256);

            using var imageStream = imageStreamResult.MemoryStream;
            var fileExtension = imageStreamResult.FileExtension;

            var imageName = emoji.Id != 0 ? emoji.Id.ToString() : emoji.Name;

            string fileName = $"{imageName}.{fileExtension}";

            var mb = new DiscordMessageBuilder()
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

    public async Task Handle(DallifyUserRequest request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var user = request.User;
        var guild = ctx.Guild;

        var message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            var userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id) ?? throw new Exception("Invalid user");

            var imageUrl = userAsMember.GuildAvatarUrl
                                ?? userAsMember.AvatarUrl
                                ?? throw new Exception("Couldn't load user avatar");

            var imageStreamResult = await DallifyImage(imageUrl, user.Id, Size512);

            using var imageStream = imageStreamResult.MemoryStream;
            var fileExtension = imageStreamResult.FileExtension;

            var imageFilename = userAsMember.DisplayName.ToSafeFilename(fileExtension);

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

    public async Task Handle(DallifyImageRequest request, CancellationToken cancellationToken)
    {
        var ctx = request.Ctx;
        var user = ctx.User;
        var message = ctx.Message;

        var imageUrl = await message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        var wokingOnItMessage = await request.Ctx.RespondAsync("Working on it");

        try
        {
            var imageStreamResult = await DallifyImage(imageUrl, user.Id, Size1024);

            using var imageStream = imageStreamResult.MemoryStream;
            var fileExtension = imageStreamResult.FileExtension;

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

    private async Task<ImageStreamResult> DallifyImage(string imageUrl, ulong userId, int maxSize)
    {
        var image = await _imageService.DownloadImage(imageUrl);

        var imageAsPng = await _imageService.ConvertImageToPng(image, MaxImageSizeInMb);

        var squaredImage = _imageService.CropToSquare(imageAsPng);

        var resultSize = Math.Min(maxSize, Math.Max(ValidSizes.Reverse().FirstOrDefault(s => squaredImage.Height >= s), ValidSizes[0]));

        var fileName = $"{Guid.NewGuid()}.png";

        var inputImageStream = await _imageService.GetImageStream(squaredImage);

        var imageVariationRequest = new CreateImageVariationRequest
        {
            Image = inputImageStream.MemoryStream.ToArray(),
            Size = $"{resultSize}x{resultSize}",
            User = userId.ToString(),
        };

        var response = await _dalleHttpClient.CreateImageVariation(imageVariationRequest, fileName);

        if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

        var imageResponseUrl = response.Data.First();

        var imageResult = await _imageService.DownloadImage(imageResponseUrl.Url);

        var imageStream = await _imageService.GetImageStream(imageResult);

        return imageStream;
    }
}
