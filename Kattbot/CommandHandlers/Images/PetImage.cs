using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Kattbot.CommandHandlers.Images;

#pragma warning disable SA1402 // File may only contain a single type
public class PetEmoteRequest : CommandRequest
{
    public PetEmoteRequest(CommandContext ctx)
    : base(ctx)
    {
    }

    public DiscordEmoji Emoji { get; set; } = null!;

    public string? Speed { get; internal set; }
}

public class PetUserRequest : CommandRequest
{
    public PetUserRequest(CommandContext ctx)
    : base(ctx)
    {
    }

    public DiscordUser User { get; set; } = null!;

    public string? Speed { get; internal set; }
}

public class PetImageHandlers : IRequestHandler<PetEmoteRequest>,
                                IRequestHandler<PetUserRequest>
{
    private readonly ImageService _imageService;
    private readonly PetPetClient _petPetClient;
    private readonly ILogger<PetImageHandlers> _logger;

    public PetImageHandlers(ImageService imageService, PetPetClient petPetClient, ILogger<PetImageHandlers> logger)
    {
        _imageService = imageService;
        _petPetClient = petPetClient;
        _logger = logger;
    }

    public async Task Handle(PetEmoteRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;

        string url = emoji.GetEmojiImageUrl();
        string imageName = emoji.Id != 0 ? emoji.Id.ToString() : emoji.Name;

        using var image = await _imageService.DownloadImage(url);

        string imagePath = await _imageService.SaveImageToTempPath(image, imageName);

        byte[] animatedEmojiBytes = await _petPetClient.PetPet(imagePath, request.Speed);

        using var outputImage = ImageService.LoadImage(animatedEmojiBytes);

        ImageStreamResult imageStreamResult = await _imageService.GetImageStream(outputImage);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile($"{imageName}.{imageStreamResult.FileExtension}", imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    public async Task Handle(PetUserRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMember? userAsMember = await ResolveGuildMember(guild, user.Id) ?? throw new Exception("Invalid user");

        string avatarUrl = userAsMember.GuildAvatarUrl ?? userAsMember.AvatarUrl;

        if (string.IsNullOrEmpty(avatarUrl))
        {
            throw new Exception("Couldn't load user avatar");
        }

        using var inputImage = await _imageService.DownloadImage(avatarUrl);

        string extension = _imageService.GetImageFileExtension(inputImage);

        string imageFilename = user.GetNicknameOrUsername().ToSafeFilename(extension);

        var croppedImage = _imageService.CropImageToCircle(inputImage);

        string imagePath = await _imageService.SaveImageToTempPath(croppedImage, imageFilename);

        byte[] animatedEmojiBytes = await _petPetClient.PetPet(imagePath, request.Speed);

        using var outputImage = ImageService.LoadImage(animatedEmojiBytes);

        var imageStreamResult = await _imageService.GetImageStream(outputImage);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile($"{imageFilename}.{imageStreamResult.FileExtension}", imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    private Task<DiscordMember?> ResolveGuildMember(DiscordGuild guild, ulong userId)
    {
        bool memberExists = guild.Members.TryGetValue(userId, out DiscordMember? member);

        return memberExists ? Task.FromResult(member) : guild.GetMemberAsync(userId);
    }
}
