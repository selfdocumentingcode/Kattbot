using Kattbot.Models;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.NotificationHandlers;

public abstract record EventNotification(EventContext Ctx) : INotification;

public record ErrorTestNotification(EventContext Ctx, string Message, int Sleep) : EventNotification(Ctx);

public class ErrorTestNotificationHandler1 : INotificationHandler<ErrorTestNotification>
{
    public async Task Handle(ErrorTestNotification notification, CancellationToken cancellationToken)
    {
        await notification.Ctx.Channel.SendMessageAsync($"H1 Start: {notification.Message}");

        if (notification.Sleep > 0) await Task.Delay(notification.Sleep);

        await notification.Ctx.Channel.SendMessageAsync($"H1 Done: {notification.Message}");

        throw new Exception(notification.Message);
    }
}

public class ErrorTestNotificationHandler2 : INotificationHandler<ErrorTestNotification>
{
    public async Task Handle(ErrorTestNotification notification, CancellationToken cancellationToken)
    {
        await notification.Ctx.Channel.SendMessageAsync($"H2 Start: {notification.Message}");

        if (notification.Sleep > 0) await Task.Delay(notification.Sleep);

        await notification.Ctx.Channel.SendMessageAsync($"H2 Done: {notification.Message}");

        throw new Exception(notification.Message);
    }
}
