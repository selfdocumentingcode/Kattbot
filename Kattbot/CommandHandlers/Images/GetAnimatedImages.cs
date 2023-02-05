using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.Images;
using MediatR;

namespace Kattbot.CommandHandlers.Images;
#pragma warning disable SA1402 // File may only contain a single type
public class GetAnimatedEmoji : CommandRequest
{
    public GetAnimatedEmoji(CommandContext ctx)
    : base(ctx)
    {
    }

    public DiscordEmoji Emoji { get; set; } = null!;
}

public class GetAnimatedUserAvatar : CommandRequest
{
    public GetAnimatedUserAvatar(CommandContext ctx)
    : base(ctx)
    {
    }

    public DiscordUser User { get; set; } = null!;
}

public class GetAnimatedImagesHandlers : IRequestHandler<GetAnimatedEmoji>,
                                            IRequestHandler<GetAnimatedUserAvatar>
{
    private readonly ImageService _imageService;
    private readonly MakeEmojiClient _makeEmojiClient;

    public GetAnimatedImagesHandlers(ImageService imageService, MakeEmojiClient makeEmojiClient)
    {
        _imageService = imageService;
        _makeEmojiClient = makeEmojiClient;
    }

    public async Task<Unit> Handle(GetAnimatedEmoji request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordEmoji emoji = request.Emoji;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string url = emoji.GetEmojiImageUrl();
            string imageName = emoji.Id != 0 ? emoji.Id.ToString() : emoji.Name;

            using ImageResult inputImageResult = await _imageService.LoadImage(url);

            string imagePath = await _imageService.SaveImageToTempPath(inputImageResult, imageName);

            byte[] animatedEmojiBytes = await _makeEmojiClient.MakeEmojiPet(imagePath);

            using ImageResult outputImageResult = ImageService.LoadImage(animatedEmojiBytes);

            ImageStreamResult imageStreamResult = await _imageService.GetImageStream(outputImageResult);

            var responseBuilder = new DiscordMessageBuilder();

            responseBuilder.AddFile($"{imageName}.{imageStreamResult.FileExtension}", imageStreamResult.MemoryStream);

            await message.DeleteAsync();

            await ctx.RespondAsync(responseBuilder);

            return Unit.Value;
        }
        catch (Exception)
        {
            await message.DeleteAsync();
            throw;
        }
    }

    public async Task<Unit> Handle(GetAnimatedUserAvatar request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
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

            using ImageResult inputImageResult = await _imageService.LoadImage(avatarUrl);

            var croppedImageResult = _imageService.CropImageToCircle(inputImageResult);

            string imagePath = await _imageService.SaveImageToTempPath(croppedImageResult, imageName);

            byte[] animatedEmojiBytes = await _makeEmojiClient.MakeEmojiPet(imagePath);

            using ImageResult outputImageResult = ImageService.LoadImage(animatedEmojiBytes);

            ImageStreamResult imageStreamResult = await _imageService.GetImageStream(outputImageResult);

            var responseBuilder = new DiscordMessageBuilder();

            responseBuilder.AddFile($"{imageName}.{imageStreamResult.FileExtension}", imageStreamResult.MemoryStream);

            await message.DeleteAsync();

            await ctx.RespondAsync(responseBuilder);

            return Unit.Value;
        }
        catch (Exception)
        {
            await message.DeleteAsync();
            throw;
        }
    }

    private Task<DiscordMember?> ResolveGuildMember(DiscordGuild guild, ulong userId)
    {
        bool memberExists = guild.Members.TryGetValue(userId, out DiscordMember? member);

        return memberExists ? Task.FromResult(member) : guild.GetMemberAsync(userId);
    }
}
