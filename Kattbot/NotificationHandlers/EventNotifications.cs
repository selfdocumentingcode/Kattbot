using MediatR;

namespace Kattbot.NotificationHandlers;

public abstract record EventNotification(EventContext Ctx) : INotification;
