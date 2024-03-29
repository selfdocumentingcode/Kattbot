using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Kattbot.NotificationHandlers.Emotes;

/// <summary>
///     Delete emote from reaction if it exists
///     Do not save emote if it does not belong to guild.
/// </summary>
public record DeleteReactionCommand : EventNotification
{
    public DeleteReactionCommand(EventContext ctx, DiscordEmoji emoji, DiscordMessage message)
        : base(ctx)
    {
        Emoji = emoji;
        Message = message;
    }

    public DiscordMessage Message { get; set; }

    public DiscordEmoji Emoji { get; set; }
}

public class DeleteReactionCommandHandler : INotificationHandler<DeleteReactionCommand>
{
    private readonly EmoteEntityBuilder _emoteBuilder;
    private readonly EmotesRepository _kattbotRepo;
    private readonly ILogger<DeleteReactionCommandHandler> _logger;

    public DeleteReactionCommandHandler(
        ILogger<DeleteReactionCommandHandler> logger,
        EmoteEntityBuilder emoteBuilder,
        EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _emoteBuilder = emoteBuilder;
        _kattbotRepo = kattbotRepo;
    }

    public Task Handle(DeleteReactionCommand request, CancellationToken cancellationToken)
    {
        EventContext ctx = request.Ctx;

        DiscordUser user = ctx.User ?? throw new Exception("User is null");

        DiscordEmoji emoji = request.Emoji;
        DiscordMessage message = request.Message;
        DiscordGuild guild = ctx.Guild;
        string username = user.Username;
        ulong userId = user.Id;

        _logger.LogDebug($"Remove reaction: {username} -> {emoji.Name}");

        if (!EmoteHelper.IsValidEmote(emoji, guild))
        {
            _logger.LogDebug($"{emoji.Name} is not valid");
            return Task.CompletedTask;
        }

        EmoteEntity emoteEntity = _emoteBuilder.BuildFromUserReaction(message, emoji, userId, guild.Id);

        _logger.LogDebug($"Removing reaction emote {emoteEntity}");

        return _kattbotRepo.RemoveEmoteEntity(emoteEntity);
    }
}
