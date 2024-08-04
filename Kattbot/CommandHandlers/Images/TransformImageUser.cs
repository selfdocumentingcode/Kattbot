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

namespace Kattbot.CommandHandlers.Images;

public class TransformImageUserRequest : CommandRequest
{
    public TransformImageUserRequest(CommandContext ctx, DiscordUser user, TransformImageEffect effect)
        : base(ctx)
    {
        User = user;
        Effect = effect;
    }

    public DiscordUser User { get; }

    public TransformImageEffect Effect { get; }
}

public class TransformImageUserHandler : IRequestHandler<TransformImageUserRequest>
{
    private readonly DiscordResolver _discordResolver;
    private readonly ImageService _imageService;

    public TransformImageUserHandler(ImageService imageService, DiscordResolver discordResolver)
    {
        _imageService = imageService;
        _discordResolver = discordResolver;
    }

    public async Task Handle(TransformImageUserRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        DiscordUser user = request.User;
        DiscordGuild guild = ctx.Guild;
        TransformImageEffect effect = request.Effect;

        DiscordMember userAsMember = await _discordResolver.ResolveGuildMember(guild, user.Id)
                                     ?? throw new Exception("Invalid user");

        string imageUrl = userAsMember.GuildAvatarUrl
                          ?? userAsMember.AvatarUrl
                          ?? throw new Exception("Couldn't load user avatar");

        ImageStreamResult imageStreamResult =
            await _imageService.TransformImage(imageUrl, effect, ImageEffects.CropToCircle);

        using MemoryStream imageStream = imageStreamResult.MemoryStream;
        string fileExtension = imageStreamResult.FileExtension;

        string imageFilename = userAsMember.DisplayName.ToSafeFilename(fileExtension);

        var responseBuilder = new DiscordMessageBuilder();

        responseBuilder.AddFile(imageFilename, imageStream);

        await ctx.RespondAsync(responseBuilder);
    }
}
