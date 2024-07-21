using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.Data.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Kattbot.NotificationHandlers.Emotes;

/// <summary>
///     Delete all messsage emotes for this message
///     Delete all reactions emotes on this message that belong to message owner
///     (Do not remove reactions emotes on this message that belong to other users).
/// </summary>
public class MessageDeletedNotificationHandler : BaseEmoteNotificationHandler,
    INotificationHandler<MessageDeletedNotification>
{
    private readonly EmotesRepository _kattbotRepo;
    private readonly ILogger<MessageDeletedNotificationHandler> _logger;

    public MessageDeletedNotificationHandler(
        ILogger<MessageDeletedNotificationHandler> logger,
        EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _kattbotRepo = kattbotRepo;
    }

    public Task Handle(MessageDeletedNotification notification, CancellationToken cancellationToken)
    {
        MessageDeletedEventArgs args = notification.EventArgs;

        DiscordMessage message = args.Message;

        if (!IsRelevantMessage(message))
        {
            _logger.LogDebug("Message is not relevant");
            return Task.CompletedTask;
        }

        ulong messageId = message.Id;

        _logger.LogDebug($"Remove emote message: {messageId}");

        return _kattbotRepo.RemoveEmotesForMessage(messageId);
    }
}
