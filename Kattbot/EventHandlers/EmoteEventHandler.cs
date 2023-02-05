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
    private readonly EventQueue _eventQueue;
    private readonly DiscordErrorLogger _discordErrorLogger;
    private readonly BotOptions _options;

    public EmoteEventHandler(
        DiscordClient client,
        ILogger<CommandEventHandler> logger,
        EventQueue eventQueue,
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

    private async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        EmoteEventContext? eventContext = null;

        try
        {
            eventContext = new EmoteEventContext()
            {
                EventName = nameof(OnMessageCreated),
                User = eventArgs.Author,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Message = eventArgs.Message,
                Source = EmoteSource.Message,
            };

            DiscordMessage message = eventArgs.Message;
            DiscordGuild guild = eventArgs.Guild;

            if (MessageIsCommand(message.Content))
            {
                return;
            }

            if (!IsReleventMessage(message))
            {
                return;
            }

            if (IsPrivateMessageChannel(message.Channel))
            {
                await message.Channel.SendMessageAsync("https://cdn.discordapp.com/emojis/740563346599968900.png?v=1");
                return;
            }

            var command = new CreateMessageCommand(eventContext, message);

            _eventQueue.Enqueue(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageCreated));

            await _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private async Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs eventArgs)
    {
        EmoteEventContext? eventContext = null;

        try
        {
            eventContext = new EmoteEventContext()
            {
                EventName = nameof(OnMessageUpdated),
                User = eventArgs.Author,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Message = eventArgs.Message,
                Source = EmoteSource.Message,
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
                return;
            }

            if (!IsReleventMessage(message))
            {
                return;
            }

            if (IsPrivateMessageChannel(channel))
            {
                return;
            }

            var command = new UpdateMessageCommand(eventContext, message);

            _eventQueue.Enqueue(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageUpdated));

            await _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private async Task OnMessageDeleted(DiscordClient client, MessageDeleteEventArgs eventArgs)
    {
        EmoteEventContext? eventContext = null;

        try
        {
            eventContext = new EmoteEventContext()
            {
                EventName = nameof(OnMessageDeleted),
                User = null,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Source = EmoteSource.Message,
            };

            DiscordChannel channel = eventArgs.Channel;
            DiscordMessage message = eventArgs.Message;
            DiscordGuild guild = eventArgs.Guild;

            if (IsPrivateMessageChannel(channel))
            {
                return;
            }

            var messageId = message.Id;

            var command = new DeleteMessageCommand(eventContext, messageId);

            _eventQueue.Enqueue(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageDeleted));

            await _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private async Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
    {
        EmoteEventContext? eventContext = null;

        try
        {
            eventContext = new EmoteEventContext()
            {
                EventName = nameof(OnMessageReactionAdded),
                User = eventArgs.User,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Source = EmoteSource.Reaction,
            };

            DiscordChannel channel = eventArgs.Channel;
            DiscordEmoji emoji = eventArgs.Emoji;
            DiscordMessage message = eventArgs.Message;
            DiscordGuild guild = eventArgs.Guild;
            DiscordUser user = eventArgs.User;

            if (IsPrivateMessageChannel(channel))
            {
                return;
            }

            if (!IsRelevantReaction(user))
            {
                return;
            }

            var command = new CreateReactionCommand(eventContext, emoji, message);

            _eventQueue.Enqueue(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageReactionAdded));

            await _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private async Task OnMessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs eventArgs)
    {
        EmoteEventContext? eventContext = null;

        try
        {
            eventContext = new EmoteEventContext()
            {
                EventName = nameof(OnMessageReactionRemoved),
                User = eventArgs.User,
                Channel = eventArgs.Channel,
                Guild = eventArgs.Guild,
                Source = EmoteSource.Reaction,
            };

            DiscordChannel channel = eventArgs.Channel;
            DiscordMessage message = eventArgs.Message;
            DiscordEmoji emoji = eventArgs.Emoji;
            DiscordGuild guild = eventArgs.Guild;
            DiscordUser user = eventArgs.User;

            if (IsPrivateMessageChannel(channel))
            {
                return;
            }

            var command = new DeleteReactionCommand(eventContext, emoji, message);

            _eventQueue.Enqueue(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(OnMessageReactionRemoved));

            await _discordErrorLogger.LogDiscordError(eventContext, ex.ToString());
        }
    }

    private bool MessageIsCommand(string command)
    {
        var commandPrefix = _options.CommandPrefix;
        var altCommandPrefix = _options.AlternateCommandPrefix;

        return command.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase)
            || command.StartsWith(altCommandPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
