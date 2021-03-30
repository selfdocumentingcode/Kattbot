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
        private readonly BotOptions _options;

        public EmoteEventHandler(
            DiscordClient client,
            ILogger<CommandEventHandler> logger,
            EmoteCommandQueue emoteCommandQueue,
            IOptions<BotOptions> options)
        {
            _client = client;
            _logger = logger;
            _emoteCommandQueue = emoteCommandQueue;
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
            }
        }

        private Task OnMessageUpdated(DiscordClient client, MessageUpdateEventArgs eventArgs)
        {
            try
            {
                var message = eventArgs.Message;
                var channel = eventArgs.Channel;
                var guild = eventArgs.Guild;

                if (!IsReleventMessage(message))
                    return Task.CompletedTask;

                if (IsPrivateMessageChannel(channel))
                    return Task.CompletedTask;

                var todoMessage = new MessageCommandPayload(message, guild);

                var command = new UpdateMessageCommand(todoMessage);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnMessageReceived");
            }

            return Task.CompletedTask;
        }

        private Task OnMessageDeleted(DiscordClient client, MessageDeleteEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var message = eventArgs.Message;
                var guild = eventArgs.Guild;

                if (IsPrivateMessageChannel(channel))
                    return Task.CompletedTask;

                var messageId = message.Id;

                var todoMessage = new MessageIdPayload(messageId, guild);

                var command = new DeleteMessageCommand(todoMessage);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnMessageDeleted");
            }

            return Task.CompletedTask;
        }

        private Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var emoji = eventArgs.Emoji;
                var message = eventArgs.Message;
                var guild = eventArgs.Guild;
                var user = eventArgs.User;

                if (IsPrivateMessageChannel(channel))
                    return Task.CompletedTask;

                if (!IsRelevantReaction(user))
                    return Task.CompletedTask;

                var todoReaction = new ReactionCommandPayload(message, emoji, user, guild);

                var command = new CreateReactionCommand(todoReaction);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnReactionAdded");
            }

            return Task.CompletedTask;
        }

        private Task OnMessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs eventArgs)
        {
            try
            {
                var channel = eventArgs.Channel;
                var message = eventArgs.Message;
                var emoji = eventArgs.Emoji;
                var guild = eventArgs.Guild;
                var user = eventArgs.User;

                if (IsPrivateMessageChannel(channel))
                    return Task.CompletedTask;

                if (!IsRelevantReaction(user))
                    return Task.CompletedTask;

                var todoReaction = new ReactionCommandPayload(message, emoji, user, guild);

                var command = new DeleteReactionCommand(todoReaction);

                _emoteCommandQueue.Enqueue(command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnReactionRemoved");
            }

            return Task.CompletedTask;
        }
    }
}
