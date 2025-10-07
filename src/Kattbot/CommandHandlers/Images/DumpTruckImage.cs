using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Common.Utils;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Kattbot.CommandHandlers.Images;

#pragma warning disable SA1402 // File may only contain a single type
public class DumpTruckEmoteRequest : CommandRequest
{
    public DumpTruckEmoteRequest(CommandContext ctx, DiscordEmoji emoji)
        : base(ctx)
    {
        Emoji = emoji;
    }

    public DiscordEmoji Emoji { get; set; }
}

public class DumpTruckUserRequest : CommandRequest
{
    public DumpTruckUserRequest(CommandContext ctx, DiscordUser user)
        : base(ctx)
    {
        User = user;
    }

    public DiscordUser User { get; set; }
}

public class DumpTruckImageRequest : CommandRequest
{
    public DumpTruckImageRequest(CommandContext ctx)
        : base(ctx)
    {
    }
}

public class DumpTruckImageHandlers : IRequestHandler<DumpTruckEmoteRequest>,
    IRequestHandler<DumpTruckUserRequest>,
    IRequestHandler<DumpTruckImageRequest>
{
    private readonly DiscordResolver _discordResolver;
    private readonly ImageService _imageService;

    public DumpTruckImageHandlers(ImageService imageService, DiscordResolver discordResolver)
    {
        _imageService = imageService;
        _discordResolver = discordResolver;
    }

    public async Task Handle(DumpTruckEmoteRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;

        string imageUrl = emoji.GetEmojiImageUrl();

        ImageStreamResult imageStreamResult = await DumpTruckImage(imageUrl);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageName = emoji.Id != 0 ? emoji.Id.ToString() : emoji.Name;

        var fileName = $"{imageName}.{fileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(fileName, imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(DumpTruckUserRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMember userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id) ??
                                     throw new Exception("Invalid user");

        string imageUrl = userAsMember.GuildAvatarUrl
                          ?? userAsMember.AvatarUrl
                          ?? throw new Exception("Couldn't load user avatar");

        ImageStreamResult imageStreamResult = await DumpTruckImage(
            imageUrl,
            ImageEffects.CropToCircle);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageFilename = userAsMember.DisplayName.ToSafeFilename(fileExtension);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(DumpTruckImageRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage message = ctx.Message;

        string? imageUrl = await message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        ImageStreamResult imageStreamResult = await DumpTruckImage(imageUrl);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var imageFilename = $"{Guid.NewGuid()}.{fileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    private async Task<ImageStreamResult> DumpTruckImage(
        string imageUrl,
        ImageTransformDelegate<Rgba32>? preTransform = null)
    {
        Image<Rgba32> inputImage = await _imageService.DownloadImage<Rgba32>(imageUrl);

        if (preTransform != null)
        {
            inputImage = preTransform(inputImage);
        }

        string dumpTruckFile = Path.Combine("Resources", "dumptruck_v1.png");
        using Image<Rgba32> dumpTruckImage = Image.Load<Rgba32>(dumpTruckFile);
        string dumpTruckMaskFile = Path.Combine("Resources", "dumptruck_v1_double_mask.png");
        using Image<Rgba32> dumpTruckMaskImage = Image.Load<Rgba32>(dumpTruckMaskFile);

        Image resultImage = FillMaskWithTilesEffect.ApplyEffect(dumpTruckImage, dumpTruckMaskImage, inputImage);

        ImageStreamResult outputImageStream = await ImageService.GetImageStream(resultImage);

        return outputImageStream;
    }
}
