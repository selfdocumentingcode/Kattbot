using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.Common.Models.Emotes;
using Kattbot.NotificationHandlers;
using Kattbot.NotificationHandlers.Emotes;
using Kattbot.Services;
using Kattbot.Workers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kattbot.EventHandlers;

public class EmoteEventHandler : BaseEventHandler
{
    private readonly DiscordClient _client;
    private readonly ILogger<CommandEventHandler> _logger;
    private readonly EventQueueChannel _eventQueue;
    private readonly DiscordErrorLogger _discordErrorLogger;
    private readonly BotOptions _options;

    public EmoteEventHandler(
        DiscordClient client,
        ILogger<CommandEventHandler> logger,
        EventQueueChannel eventQueue,
        IOptions<BotOptions> options,
        DiscordErrorLogger discordErrorLogger)
    {
        _client = client;
        _logger = logger;
        _eventQueue = eventQueue;
        _discordErrorLogger = discordErrorLogger;
        _options = options.Value;
    }

    public void RegisterHandlers()
    {
        _client.MessageCreated += OnMessageCreated;
        _client.MessageDeleted += OnMessageDeleted;
        _client.MessageUpdated += OnMessageUpdated;

        _client.MessageReactionAdded += OnMessageReactionAdded;
        _client.MessageReactionRemoved += OnMessageReactionRemoved;
    }

    private Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        EventContext? eventContext = null;

        try
        {
            eventContext = new EventContext()
            {
                EventName = nameof(OnMessageCreated),
                User = eventArgs.Author,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Message = eventArgs.Message,
            };

            DiscordMessage message = eventArgs.Message;
            DiscordGuild guild = eventArgs.Guild;

            if (MessageIsCommand(message.Content))
            {
                return Task.CompletedTask;
            }

            if (!IsReleventMessage(message))
            {
                return Task.CompletedTask;
            }

            if (IsPrivateMessageChannel(message.Channel))
            {
                return message.Channel.SendMessageAsync("https://cdn.discordapp.com/emojis/740563346599968900.png?v=1");
            }

            var command = new CreateMessageCommand(eventContext, message);

            return _eventQueue.Writer.WriteAsync(command).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageCreated));

            return _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs eventArgs)
    {
        EventContext? eventContext = null;

        try
        {
            eventContext = new EventContext()
            {
                EventName = nameof(OnMessageUpdated),
                User = eventArgs.Author,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Message = eventArgs.Message
            };

            DiscordMessage message = eventArgs.Message;
            DiscordChannel channel = eventArgs.Channel;
            DiscordGuild guild = eventArgs.Guild;

            if (message == null)
            {
                throw new Exception($"{nameof(eventArgs.Message)} is null");
            }

            // issues caused by Threads feature
            if (message.Author == null)
            {
                return Task.CompletedTask;
            }

            if (!IsReleventMessage(message))
            {
                return Task.CompletedTask;
            }

            if (IsPrivateMessageChannel(channel))
            {
                return Task.CompletedTask;
            }

            var command = new UpdateMessageCommand(eventContext, message);

            return _eventQueue.Writer.WriteAsync(command).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageUpdated));

            return _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private Task OnMessageDeleted(DiscordClient client, MessageDeleteEventArgs eventArgs)
    {
        EventContext? eventContext = null;

        try
        {
            eventContext = new EventContext()
            {
                EventName = nameof(OnMessageDeleted),
                User = null,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
            };

            DiscordChannel channel = eventArgs.Channel;
            DiscordMessage message = eventArgs.Message;
            DiscordGuild guild = eventArgs.Guild;

            if (IsPrivateMessageChannel(channel))
            {
                return Task.CompletedTask;
            }

            ulong messageId = message.Id;

            var command = new DeleteMessageCommand(eventContext, messageId);

            return _eventQueue.Writer.WriteAsync(command).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageDeleted));

            return _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        EventContext? eventContext = null;

        try
        {
            eventContext = new EventContext()
            {
                EventName = nameof(OnMessageReactionAdded),
                User = eventArgs.User,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
            };

            DiscordChannel channel = eventArgs.Channel;
            DiscordEmoji emoji = eventArgs.Emoji;
            DiscordMessage message = eventArgs.Message;
            DiscordGuild guild = eventArgs.Guild;
            DiscordUser user = eventArgs.User;

            if (IsPrivateMessageChannel(channel))
            {
                return Task.CompletedTask;
            }

            if (!IsRelevantReaction(user))
            {
                return Task.CompletedTask;
            }

            var command = new CreateReactionCommand(eventContext, emoji, message);

            return _eventQueue.Writer.WriteAsync(command).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageReactionAdded));

            return _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private Task OnMessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs eventArgs)
    {
        EventContext? eventContext = null;

        try
        {
            eventContext = new EventContext()
            {
                EventName = nameof(OnMessageReactionRemoved),
                User = eventArgs.User,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
            };

            DiscordChannel channel = eventArgs.Channel;
            DiscordMessage message = eventArgs.Message;
            DiscordEmoji emoji = eventArgs.Emoji;
            DiscordGuild guild = eventArgs.Guild;
            DiscordUser user = eventArgs.User;

            if (IsPrivateMessageChannel(channel))
            {
                return Task.CompletedTask;
            }

            var command = new DeleteReactionCommand(eventContext, emoji, message);

            return _eventQueue.Writer.WriteAsync(command).AsTask();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageReactionRemoved));

            return _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private bool MessageIsCommand(string command)
    {
        string commandPrefix = _options.CommandPrefix;
        string altCommandPrefix = _options.AlternateCommandPrefix;

        return command.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase)
            || command.StartsWith(altCommandPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
