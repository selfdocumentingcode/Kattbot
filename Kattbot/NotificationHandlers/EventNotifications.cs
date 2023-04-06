using DSharpPlus.EventArgs;
using MediatR;

namespace Kattbot.NotificationHandlers;

public abstract record EventNotification(EventContext Ctx) : INotification;

// TODO clean this up by removing EventContext from base contructor entirely
// or at least move the mapping somewhere else
public record MessageCreatedNotification(MessageCreateEventArgs EventArgs)
    : EventNotification(new EventContext()
    {
        Channel = EventArgs.Channel,
        Guild = EventArgs.Guild,
        User = EventArgs.Author,
        Message = EventArgs.Message,
        EventName = "MessageCreated",
    });
