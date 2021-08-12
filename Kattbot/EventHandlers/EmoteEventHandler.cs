using DSharpPlus;
using DSharpPlus.EventArgs;
using Kattbot.Models;
using Kattbot.Models.Commands;
using Kattbot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Kattbot.EventHandlers
{
    public class EmoteEventHandler : BaseEventHandler
    {
        private readonly DiscordClient _client;
        private readonly ILogger<CommandEventHandler> _logger;
        private readonly EmoteCommandQueue _emoteCommandQueue;
        private readonly DiscordErrorLogger _discordErrorLogger;
        private readonly BotOptions _options;

        public EmoteEventHandler(
            DiscordClient client,
            ILogger<CommandEventHandler> logger,
            EmoteCommandQueue emoteCommandQueue,
            IOptions<BotOptions> options,
            DiscordErrorLogger discordErrorLogger)
        {
            _client = client;
            _logger = logger;
            _emoteCommandQueue = emoteCommandQueue;
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
            try
            {
                var message = eventArgs.Message;
                var guild = eventArgs.Guild;

                if (MessageIsCommand(message.Content))
                    return;

                if (!IsReleventMessage(message))
                    return;

                if (IsPrivateMessageChannel(message.Channel))
                {
                    await message.Channel.SendMessageAsync("https://cdn.discordapp.com/emojis/740563346599968900.png?v=1");
                    return;
                }

                var todoMessage = new MessageCommandPayload(message, guild);

                var command = new CreateMessageCommand(todoMessage);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageCreated));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageCreated),
                    User = eventArgs.Author,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild,
                    Message = eventArgs.Message
                };

                await _discordErrorLogger.LogDiscordError(eventContextError, ex.ToString());
            }
        }

        private async Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs eventArgs)
        {
            try
            {
                var message = eventArgs.Message;
                var channel = eventArgs.Channel;
                var guild = eventArgs.Guild;

                if (message == null)
                    throw new Exception($"{nameof(eventArgs.Message)} is null");

                // issues caused by Threads feature
                if (message.Author == null)                
                    return;
                    
                if (!IsReleventMessage(message))
                    return;

                if (IsPrivateMessageChannel(channel))
                    return;

                var todoMessage = new MessageCommandPayload(message, guild);

                var command = new UpdateMessageCommand(todoMessage);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageUpdated));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageUpdated),
                    User = eventArgs.Author,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild,
                    Message = eventArgs.Message
                };

                await _discordErrorLogger.LogDiscordError(eventContextError, ex.ToString());
            }
        }

        private async Task OnMessageDeleted(DiscordClient client, MessageDeleteEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var message = eventArgs.Message;
                var guild = eventArgs.Guild;

                if (IsPrivateMessageChannel(channel))
                    return;

                var messageId = message.Id;

                var todoMessage = new MessageIdPayload(messageId, guild);

                var command = new DeleteMessageCommand(todoMessage);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageDeleted));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageDeleted),
                    User = null,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild
                };

                await _discordErrorLogger.LogDiscordError(eventContextError, ex.ToString());
            }
        }

        private async Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var emoji = eventArgs.Emoji;
                var message = eventArgs.Message;
                var guild = eventArgs.Guild;
                var user = eventArgs.User;

                if (IsPrivateMessageChannel(channel))
                    return;

                if (!IsRelevantReaction(user))
                    return;

                var todoReaction = new ReactionCommandPayload(message, emoji, user, guild);

                var command = new CreateReactionCommand(todoReaction);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageReactionAdded));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageReactionAdded),
                    User = eventArgs.User,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild
                };

                await _discordErrorLogger.LogDiscordError(eventContextError, ex.ToString());
            }
        }

        private async Task OnMessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var message = eventArgs.Message;
                var emoji = eventArgs.Emoji;
                var guild = eventArgs.Guild;
                var user = eventArgs.User;

                if (IsPrivateMessageChannel(channel))
                    return;

                var todoReaction = new ReactionCommandPayload(message, emoji, user, guild);

                var command = new DeleteReactionCommand(todoReaction);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(OnMessageReactionRemoved));

                var eventContextError = new EventErrorContext()
                {
                    EventName = nameof(OnMessageReactionRemoved),
                    User = eventArgs.User,
                    Channel = eventArgs.Channel,
                    Guild = eventArgs.Guild
                };

                await _discordErrorLogger.LogDiscordError(eventContextError, ex.ToString());
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
}
