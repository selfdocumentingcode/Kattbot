using System.Collections.Generic;
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
///     Delete all messsage emotes for this message
///     Extract emotes from message text
///     If message contains emotes, save each emote
///     Do save emote if it does not belong to guild.
/// </summary>
public class MessageUpdatedNotificationHandler : BaseNotificationHandler,
    INotificationHandler<MessageUpdatedNotification>
{
    private readonly EmotesRepository _kattbotRepo;
    private readonly ILogger<MessageUpdatedNotificationHandler> _logger;

    public MessageUpdatedNotificationHandler(
        ILogger<MessageUpdatedNotificationHandler> logger,
        EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _kattbotRepo = kattbotRepo;
    }

    public async Task Handle(MessageUpdatedNotification notification, CancellationToken cancellationToken)
    {
        MessageUpdatedEventArgs args = notification.EventArgs;

        DiscordMessage message = args.Message;
        DiscordGuild guild = args.Guild;

        ulong messageId = message.Id;
        string messageContent = message.Content;
        string username = message.Author?.Username ?? "Unknown";

        _logger.LogDebug($"Update emote message: {username} -> {messageContent}");

        if (!IsRelevantMessage(message))
        {
            _logger.LogDebug("Message is not relevant");
            return;
        }

        ulong guildId = guild.Id;

        await _kattbotRepo.RemoveEmotesForMessage(messageId);

        List<EmoteEntity> emotes = EmoteEntityBuilder.BuildFromSocketUserMessage(message, guildId);

        if (emotes.Count > 0)
        {
            _logger.LogDebug($"Message contains {emotes.Count} emotes", emotes);

            foreach (EmoteEntity emote in emotes)
            {
                if (!EmoteHelper.IsValidEmote(emote, guild))
                {
                    _logger.LogDebug($"{emote} is not valid");
                    continue;
                }

                _logger.LogDebug($"Saving message emote {emote}");

                await _kattbotRepo.CreateEmoteEntity(emote);
            }
        }
        else
        {
            _logger.LogDebug("Message contains no emotes");
        }
    }
}
