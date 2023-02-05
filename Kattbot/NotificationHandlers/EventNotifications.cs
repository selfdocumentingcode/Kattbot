using MediatR;

namespace Kattbot.NotificationHandlers;

public abstract record EventNotification(EventContext Ctx) : INotification;

public abstract record EmoteEventNotification(EmoteEventContext Ctx) : INotification;
