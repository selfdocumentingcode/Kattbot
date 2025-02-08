using DSharpPlus.EventArgs;
using MediatR;

namespace Kattbot.NotificationHandlers;

public abstract record EventNotification : INotification;

#pragma warning disable SA1402 // File may only contain a single type
public record MessageReactionAddedNotification(MessageReactionAddedEventArgs EventArgs) : EventNotification;

public record MessageReactionRemovedNotification(MessageReactionRemovedEventArgs EventArgs) : EventNotification;

public record MessageCreatedNotification(MessageCreatedEventArgs EventArgs) : EventNotification;

public record MessageUpdatedNotification(MessageUpdatedEventArgs EventArgs) : EventNotification;

public record MessageDeletedNotification(MessageDeletedEventArgs EventArgs) : EventNotification;

public record MessageBulkDeletedNotification(MessagesBulkDeletedEventArgs EventArgs) : EventNotification;
