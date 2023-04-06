﻿using System;
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
public class GetAnimatedEmoji : CommandRequest
{
    public GetAnimatedEmoji(CommandContext ctx)
    : base(ctx)
    {
    }

    public DiscordEmoji Emoji { get; set; } = null!;

    public string? Speed { get; internal set; }
}

public class GetAnimatedUserAvatar : CommandRequest
{
    public GetAnimatedUserAvatar(CommandContext ctx)
    : base(ctx)
    {
    }

    public DiscordUser User { get; set; } = null!;

    public string? Speed { get; internal set; }
}

public class GetAnimatedImagesHandlers : IRequestHandler<GetAnimatedEmoji>,
                                            IRequestHandler<GetAnimatedUserAvatar>
{
    private readonly ImageService _imageService;
    private readonly PetPetClient _petPetClient;
    private readonly ILogger<GetAnimatedImagesHandlers> _logger;

    public GetAnimatedImagesHandlers(ImageService imageService, PetPetClient petPetClient, ILogger<GetAnimatedImagesHandlers> logger)
    {
        _imageService = imageService;
        _petPetClient = petPetClient;
        _logger = logger;
    }

    public async Task Handle(GetAnimatedEmoji request, CancellationToken cancellationToken)
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

    public async Task Handle(GetAnimatedUserAvatar request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMember? userAsMember = await ResolveGuildMember(guild, user.Id);

        if (userAsMember == null)
        {
            throw new Exception("Invalid user");
        }

        string avatarUrl = userAsMember.GuildAvatarUrl ?? userAsMember.AvatarUrl;

        if (string.IsNullOrEmpty(avatarUrl))
        {
            throw new Exception("Couldn't load user avatar");
        }

        // TODO find a nicer filename
        string imageName = user.Id.ToString();

        using var inputImage = await _imageService.DownloadImage(avatarUrl);

        var croppedImage = _imageService.CropImageToCircle(inputImage);

        string imagePath = await _imageService.SaveImageToTempPath(croppedImage, imageName);

        byte[] animatedEmojiBytes = await _petPetClient.PetPet(imagePath, request.Speed);

        using var outputImage = ImageService.LoadImage(animatedEmojiBytes);

        ImageStreamResult imageStreamResult = await _imageService.GetImageStream(outputImage);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile($"{imageName}.{imageStreamResult.FileExtension}", imageStreamResult.MemoryStream);

        await ctx.RespondAsync(responseBuilder);
    }

    private Task<DiscordMember?> ResolveGuildMember(DiscordGuild guild, ulong userId)
    {
        bool memberExists = guild.Members.TryGetValue(userId, out DiscordMember? member);

        return memberExists ? Task.FromResult(member) : guild.GetMemberAsync(userId);
    }
}
