using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.Services;

public class NotificationPublisher
{
    private readonly NotificationMediator _mediator;

    public NotificationPublisher(IServiceProvider serviceProvider)
    {
        ServiceFactory factory = serviceProvider.GetService!;
        _mediator = new NotificationMediator(factory, SyncContinueOnException);
    }

    public Task Publish(INotification notification, CancellationToken cancellationToken) => _mediator.Publish(notification, cancellationToken);

    /// <summary>
    /// Sauce: https://github.com/jbogard/MediatR/blob/master/samples/MediatR.Examples.PublishStrategies/Publisher.cs
    /// </summary>
    private async Task SyncContinueOnException(IEnumerable<Func<INotification, CancellationToken, Task>> handlers, INotification notification, CancellationToken cancellationToken)
    {
        var exceptions = new List<Exception>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler(notification, cancellationToken).ConfigureAwait(false);
            }
            catch (AggregateException ex)
            {
                exceptions.AddRange(ex.Flatten().InnerExceptions);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Any())
        {
            throw new AggregateException(exceptions);
        }
    }
}

public class NotificationMediator : Mediator
{
    private Func<IEnumerable<Func<INotification, CancellationToken, Task>>, INotification, CancellationToken, Task> _publish;

    public NotificationMediator(ServiceFactory serviceFactory, Func<IEnumerable<Func<INotification, CancellationToken, Task>>, INotification, CancellationToken, Task> publish) : base(serviceFactory)
    {
        _publish = publish;
    }

    protected override Task PublishCore(IEnumerable<Func<INotification, CancellationToken, Task>> allHandlers, INotification notification, CancellationToken cancellationToken)
    {
        return _publish(allHandlers, notification, cancellationToken);
    }
}

