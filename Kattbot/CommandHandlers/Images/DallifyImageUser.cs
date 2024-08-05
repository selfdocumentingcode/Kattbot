using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Common.Utils;
using Kattbot.Helpers;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using MediatR;

namespace Kattbot.CommandHandlers.Images;

public class DallifyImageUserRequest : CommandRequest
{
    public DallifyImageUserRequest(CommandContext ctx, DiscordUser user)
        : base(ctx)
    {
        User = user;
    }

    public DiscordUser User { get; }
}

public class DallifyImageUserHandler : DallifyImageHandlerBase,
    IRequestHandler<DallifyImageUserRequest>
{
    private readonly DiscordResolver _discordResolver;

    public DallifyImageUserHandler(
        DalleHttpClient dalleHttpClient,
        ImageService imageService,
        DiscordResolver discordResolver)
        : base(dalleHttpClient, imageService)
    {
        _discordResolver = discordResolver;
    }

    public async Task Handle(DallifyImageUserRequest request, CancellationToken cancellationToken)
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
}
