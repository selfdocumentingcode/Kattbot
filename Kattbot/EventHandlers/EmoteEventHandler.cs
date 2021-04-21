using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.Models;
using Kattbot.Models.Commands;
using Kattbot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                var commandPrefix = _options.CommandPrefix;
                var altCommandPrefix = _options.AlternateCommandPrefix;

                var socketMessage = eventArgs.Message;
                var guild = eventArgs.Guild;

                if (socketMessage.Content.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase)
                    || socketMessage.Content.StartsWith(altCommandPrefix, StringComparison.OrdinalIgnoreCase))
                    return;

                if (!IsReleventMessage(socketMessage))
                    return;

                if (IsPrivateMessageChannel(socketMessage.Channel))
                {
                    await socketMessage.Channel.SendMessageAsync("https://cdn.discordapp.com/emojis/740563346599968900.png?v=1");
                    return;
                }

                var message = socketMessage;

                var todoMessage = new MessageCommandPayload(message, guild);

                var command = new CreateMessageCommand(todoMessage);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnMessageReceived");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
            }
        }

        private async Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs eventArgs)
        {
            try
            {
                var message = eventArgs.Message;
                var channel = eventArgs.Channel;
                var guild = eventArgs.Guild;

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
                _logger.LogError(ex, "OnMessageReceived");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
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
                _logger.LogError(ex, "OnMessageDeleted");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
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
                _logger.LogError(ex, "OnReactionAdded");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
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
                _logger.LogError(ex, "OnReactionRemoved");
                await _discordErrorLogger.LogDiscordError(ex.ToString());
            }
        }
    }
}
