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
///     Save emote from reaction
///     Do not save emote if it does not belong to guild.
/// </summary>
public class MessageReactionAddedNotificationHandler : BaseNotificationHandler,
    INotificationHandler<MessageReactionAddedNotification>
{
    private readonly EmotesRepository _kattbotRepo;
    private readonly ILogger<MessageReactionAddedNotificationHandler> _logger;

    public MessageReactionAddedNotificationHandler(
        ILogger<MessageReactionAddedNotificationHandler> logger,
        EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _kattbotRepo = kattbotRepo;
    }

    public Task Handle(MessageReactionAddedNotification notification, CancellationToken cancellationToken)
    {
        MessageReactionAddedEventArgs args = notification.EventArgs;

        DiscordUser user = args.User;
        DiscordMessage message = args.Message;
        DiscordEmoji emoji = args.Emoji;
        DiscordGuild guild = args.Guild;

        string username = user.Username;
        ulong userId = user.Id;

        _logger.LogDebug($"Reaction: {username} -> {emoji.Name}");

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

        EmoteEntity emoteEntity = EmoteEntityBuilder.BuildFromUserReaction(message, emoji, userId, guild.Id);

        _logger.LogDebug($"Saving reaction emote {emoteEntity}");

        return _kattbotRepo.CreateEmoteEntity(emoteEntity);
    }
}
