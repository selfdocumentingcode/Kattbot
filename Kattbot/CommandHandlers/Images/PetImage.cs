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
public class PetEmoteRequest : CommandRequest
{
    public PetEmoteRequest(CommandContext ctx, DiscordEmoji emoji, string? speed)
        : base(ctx)
    {
        Emoji = emoji;
        Speed = speed;
    }

    public DiscordEmoji Emoji { get; set; }

    public string? Speed { get; set; }
}

public class PetUserRequest : CommandRequest
{
    public PetUserRequest(CommandContext ctx, DiscordUser user, string? speed)
        : base(ctx)
    {
        User = user;
        Speed = speed;
    }

    public DiscordUser User { get; set; }

    public string? Speed { get; set; }
}

public class PetImageRequest : CommandRequest
{
    public PetImageRequest(CommandContext ctx, string? speed)
        : base(ctx)
    {
        Speed = speed;
    }

    public string? Speed { get; set; }
}

public class PetImageHandlers : IRequestHandler<PetEmoteRequest>,
    IRequestHandler<PetUserRequest>,
    IRequestHandler<PetImageRequest>
{
    private readonly DiscordResolver _discordResolver;
    private readonly ImageService _imageService;
    private readonly PetPetClient _petPetClient;

    public PetImageHandlers(ImageService imageService, PetPetClient petPetClient, DiscordResolver discordResolver)
    {
        _imageService = imageService;
        _petPetClient = petPetClient;
        _discordResolver = discordResolver;
    }

    public async Task Handle(PetEmoteRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;

        string imageUrl = emoji.GetEmojiImageUrl();

        ImageStreamResult imageStreamResult = await PetImage(imageUrl, request.Speed);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageName = emoji.Id != 0 ? emoji.Id.ToString() : emoji.Name;

        var fileName = $"{imageName}.{imageStreamResult.FileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(fileName, imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(PetImageRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordMessage message = ctx.Message;

        string? imageUrl = await message.GetImageUrlFromMessage();

        if (imageUrl == null)
        {
            await ctx.RespondAsync("I didn't find any images.");
            return;
        }

        ImageStreamResult imageStreamResult = await PetImage(imageUrl, request.Speed);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        var imageFilename = $"{Guid.NewGuid()}.{fileExtension}";

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(PetUserRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMember userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id) ??
                                     throw new Exception("Invalid user");

        string imageUrl = userAsMember.GuildAvatarUrl
                          ?? userAsMember.AvatarUrl
                          ?? throw new Exception("Couldn't load user avatar");

        ImageStreamResult imageStreamResult = await PetImage(imageUrl, request.Speed, _imageService.CropToCircle);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageFilename = userAsMember.DisplayName.ToSafeFilename(fileExtension);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    private async Task<ImageStreamResult> PetImage(
        string imageUrl,
        string? speed,
        ImageTransformDelegate<Rgba32>? preTransform = null)
    {
        Image<Rgba32> inputImage = await _imageService.DownloadImage<Rgba32>(imageUrl);

        if (preTransform != null)
        {
            inputImage = preTransform(inputImage);
        }

        string extension = _imageService.GetImageFileExtension(inputImage);

        string imagePath = await _imageService.SaveImageToTempPath(inputImage, $"{Guid.NewGuid()}.{extension}");

        byte[] animatedEmojiBytes = await _petPetClient.PetPet(imagePath, speed);

        Image outputImage = ImageService.LoadImage(animatedEmojiBytes);

        ImageStreamResult ouputImageStream = await _imageService.GetImageStream(outputImage);

        return ouputImageStream;
    }
}
