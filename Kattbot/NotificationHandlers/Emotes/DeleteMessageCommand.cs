using System.Threading;
using System.Threading.Tasks;
using Kattbot.Data;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Kattbot.NotificationHandlers.Emotes;

/// <summary>
/// Delete all messsage emotes for this message
/// Delete all reactions emotes on this message that belong to message owner
/// (Do not remove reactions emotes on this message that belong to other users).
/// </summary>
public record DeleteMessageCommand : EventNotification
{
    public DeleteMessageCommand(EventContext ctx, ulong messageId)
        : base(ctx)
    {
        MessageId = messageId;
    }

    public ulong MessageId { get; set; }
}

public class DeleteMessageCommandHandler : INotificationHandler<DeleteMessageCommand>
{
    private readonly ILogger<DeleteMessageCommandHandler> _logger;
    private readonly EmotesRepository _kattbotRepo;

    public DeleteMessageCommandHandler(ILogger<DeleteMessageCommandHandler> logger, EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _kattbotRepo = kattbotRepo;
    }

    public Task Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        ulong messageId = request.MessageId;

        _logger.LogDebug($"Remove emote message: {messageId}");

        return _kattbotRepo.RemoveEmotesForMessage(messageId);
    }
}
