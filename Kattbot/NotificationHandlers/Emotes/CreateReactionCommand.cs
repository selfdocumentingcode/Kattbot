using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Helpers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Kattbot.NotificationHandlers.Emotes;

/// <summary>
/// Save emote from reaction
/// Do not save emote if it does not belong to guild.
/// </summary>
public record CreateReactionCommand : EventNotification
{
    public CreateReactionCommand(EventContext ctx, DiscordEmoji emoji, DiscordMessage message)
        : base(ctx)
    {
        Emoji = emoji;
        Message = message;
    }

    public DiscordMessage Message { get; set; }

    public DiscordEmoji Emoji { get; set; }
}

public class CreateReactionCommandHandler : INotificationHandler<CreateReactionCommand>
{
    private readonly ILogger<CreateReactionCommandHandler> _logger;
    private readonly EmoteEntityBuilder _emoteBuilder;
    private readonly EmotesRepository _kattbotRepo;

    public CreateReactionCommandHandler(ILogger<CreateReactionCommandHandler> logger, EmoteEntityBuilder emoteBuilder, EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _emoteBuilder = emoteBuilder;
        _kattbotRepo = kattbotRepo;
    }

    public Task Handle(CreateReactionCommand request, CancellationToken cancellationToken)
    {
        EventContext ctx = request.Ctx;

        DiscordUser user = ctx.User ?? throw new Exception("User is null");

        DiscordMessage message = request.Message;
        DiscordEmoji emoji = request.Emoji;
        DiscordGuild guild = ctx.Guild;
        string username = user.Username;
        ulong userId = user.Id;

        _logger.LogDebug($"Reaction: {username} -> {emoji.Name}");

        if (!EmoteHelper.IsValidEmote(emoji, guild))
        {
            _logger.LogDebug($"{emoji.Name} is not valid");
            return Task.CompletedTask;
        }

        EmoteEntity emoteEntity = _emoteBuilder.BuildFromUserReaction(message, emoji, userId, guild.Id);

        _logger.LogDebug($"Saving reaction emote {emoteEntity}");

        return _kattbotRepo.CreateEmoteEntity(emoteEntity);
    }
}
