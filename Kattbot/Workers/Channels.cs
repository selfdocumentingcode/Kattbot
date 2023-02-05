using System.Threading.Channels;
using Kattbot.CommandHandlers;
using MediatR;

#pragma warning disable SA1402 // File may only contain a single type

namespace Kattbot.Workers;

public interface IQueueChannel<T>
    where T : class
{
    public ChannelWriter<T> Writer { get; }

    public ChannelReader<T> Reader { get; }
}

public abstract class AbstractQueueChannel<T> : IQueueChannel<T>
    where T : class
{
    private readonly Channel<T> _channel;

    public AbstractQueueChannel(Channel<T> channel)
    {
        _channel = channel;
    }

    public ChannelWriter<T> Writer => _channel.Writer;

    public ChannelReader<T> Reader => _channel.Reader;
}

public class CommandQueueChannel : AbstractQueueChannel<CommandRequest>
{
    public CommandQueueChannel(Channel<CommandRequest> channel)
        : base(channel)
    {
    }
}

public class CommandParallelQueueChannel : AbstractQueueChannel<CommandRequest>
{
    public CommandParallelQueueChannel(Channel<CommandRequest> channel)
        : base(channel)
    {
    }
}

public class EventQueueChannel : AbstractQueueChannel<INotification>
{
    public EventQueueChannel(Channel<INotification> channel)
        : base(channel)
    {
    }
}
