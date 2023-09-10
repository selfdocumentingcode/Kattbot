using System;
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

#pragma warning disable SA1402 // File may only contain a single type
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

public class DallifyImageHandler : IRequestHandler<DallifyEmoteRequest>,
                                    IRequestHandler<DallifyUserRequest>
{
    private const string Size = "256x256";

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
        DiscordEmoji emoji = request.Emoji;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            string url = emoji.GetEmojiImageUrl();

            using var emojiImage = await _imageService.DownloadImage(url);

            var emojiImageAsPng = await ImageService.ConvertImageToPng(emojiImage);

            var squaredEmojiImage = await _imageService.SquareImage(emojiImageAsPng);

            string fileName = $"{Guid.NewGuid()}.png";

            var imageVariationRequest = new CreateImageVariationRequest
            {
                Image = squaredEmojiImage.MemoryStream.ToArray(),
                Size = Size,
                User = request.Ctx.User.Id.ToString(),
            };

            var response = await _dalleHttpClient.CreateImageVariation(imageVariationRequest, fileName);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            var imageUrl = response.Data.First();

            var image = await _imageService.DownloadImage(imageUrl.Url);

            var imageStream = await _imageService.GetImageStream(image);

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(fileName, imageStream.MemoryStream)
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
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;

        DiscordMessage message = await request.Ctx.RespondAsync("Working on it");

        try
        {
            DiscordMember? userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id) ?? throw new Exception("Invalid user");

            string avatarUrl = userAsMember.GuildAvatarUrl
                                ?? userAsMember.AvatarUrl
                                ?? throw new Exception("Couldn't load user avatar");

            using var inputImage = await _imageService.DownloadImage(avatarUrl);

            var avatarAsPng = await ImageService.ConvertImageToPng(inputImage);

            var avatarImageStream = await _imageService.GetImageStream(avatarAsPng);

            string imageFilename = user.GetNicknameOrUsername().ToSafeFilename(avatarImageStream.FileExtension);

            var imageVariationRequest = new CreateImageVariationRequest
            {
                Image = avatarImageStream.MemoryStream.ToArray(),
                Size = Size,
                User = request.Ctx.User.Id.ToString(),
            };

            var response = await _dalleHttpClient.CreateImageVariation(imageVariationRequest, imageFilename);

            if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

            var imageUrl = response.Data.First();

            var image = await _imageService.DownloadImage(imageUrl.Url);

            var imageStream = await _imageService.GetImageStream(image);

            DiscordMessageBuilder mb = new DiscordMessageBuilder()
                .AddFile(imageFilename, imageStream.MemoryStream)
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
}
