using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
public class MessageReactionRemovedNotificationHandler : BaseNotificationHandler,
    INotificationHandler<MessageReactionRemovedNotification>
{
    private readonly EmoteEntityBuilder _emoteBuilder;
    private readonly EmotesRepository _kattbotRepo;
    private readonly ILogger<MessageReactionRemovedNotificationHandler> _logger;

    public MessageReactionRemovedNotificationHandler(
        ILogger<MessageReactionRemovedNotificationHandler> logger,
        EmoteEntityBuilder emoteBuilder,
        EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _emoteBuilder = emoteBuilder;
        _kattbotRepo = kattbotRepo;
    }

    public Task Handle(MessageReactionRemovedNotification notification, CancellationToken cancellationToken)
    {
        MessageReactionRemovedEventArgs args = notification.EventArgs;

        DiscordUser user = args.User;
        DiscordEmoji emoji = args.Emoji;
        DiscordMessage message = args.Message;
        DiscordGuild guild = args.Guild;

        string username = user.Username;
        ulong userId = user.Id;

        _logger.LogDebug($"Remove reaction: {username} -> {emoji.Name}");

        if (!IsRelevantAuthor(user))
        {
            _logger.LogDebug("Author is not relevant");
            return Task.CompletedTask;
        }

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
