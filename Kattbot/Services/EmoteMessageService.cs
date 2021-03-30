using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data;
using Kattbot.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Services
{
    public class EmoteMessageService
    {
        private readonly ILogger<EmoteMessageService> _logger;
        private readonly EmoteEntityBuilder _emoteBuilder;
        private readonly EmotesRepository _kattbotRepo;

        public EmoteMessageService(
            ILogger<EmoteMessageService> logger,
            EmoteEntityBuilder emoteBuilder,
            EmotesRepository kattbotRepo
            )
        {
            _logger = logger;
            _emoteBuilder = emoteBuilder;
            _kattbotRepo = kattbotRepo;
        }

        /// <summary>
        /// Extract emotes from message text
        /// If message contains emotes, save each emote 
        /// Do save emote if it does not belong to guild
        /// </summary>
        /// <param name="todoMessage"></param>
        /// <returns></returns>
        public async Task<bool> CreateEmoteMessage(MessageCommandPayload todoMessage)
        {
            try
            {
                var message = todoMessage.Message;
                var guild = todoMessage.Guild;

                var messageId = message.Id;
                var messageContent = message.Content;
                var username = message.Author.Username;

                _logger.LogDebug($"Emote message: {username} -> {messageContent}");

                var guildId = guild.Id;

                var emotes = _emoteBuilder.BuildFromSocketUserMessage(message, guildId);

                if (emotes.Count > 0)
                {
                    _logger.LogDebug($"Message contains {emotes.Count} emotes", emotes);

                    foreach (var emote in emotes)
                    {
                        if (!IsValidEmote(emote, guild))
                        {
                            _logger.LogDebug($"{emote} is not valid");
                            continue;
                        }

                        _logger.LogDebug($"Saving message emote {emote}");

                        await _kattbotRepo.CreateEmoteEntity(emote);
                    }
                }
                else
                {
                    _logger.LogDebug("Message contains no emotes");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateEmoteMessage");
                return false;
            }
        }

        /// <summary>
        /// Delete all messsage emotes for this message
        /// Extract emotes from message text
        /// If message contains emotes, save each emote 
        /// Do save emote if it does not belong to guild
        /// </summary>
        /// <param name="todoMessage"></param>
        /// <returns></returns>
        public async Task<bool> UpdateEmoteMessage(MessageCommandPayload todoMessage)
        {
            try
            {
                var message = todoMessage.Message;
                var guild = todoMessage.Guild;

                var messageId = message.Id;
                var messageContent = message.Content;
                var username = message.Author.Username;

                _logger.LogDebug($"Update emote message: {username} -> {messageContent}");

                var guildId = guild.Id;

                await _kattbotRepo.RemoveEmotesForMessage(messageId);

                var emotes = _emoteBuilder.BuildFromSocketUserMessage(message, guildId);

                if (emotes.Count > 0)
                {
                    _logger.LogDebug($"Message contains {emotes.Count} emotes", emotes);

                    foreach (var emote in emotes)
                    {
                        if (!IsValidEmote(emote, guild))
                        {
                            _logger.LogDebug($"{emote} is not valid");
                            continue;
                        }

                        _logger.LogDebug($"Saving message emote {emote}");

                        await _kattbotRepo.CreateEmoteEntity(emote);
                    }
                }
                else
                {
                    _logger.LogDebug("Message contains no emotes");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateEmoteMessage");
                return false;
            }
        }

        /// <summary>
        /// Delete all messsage emotes for this message
        /// Delete all reactions emotes on this message that belong to message owner
        /// (Do not remove reactions emotes on this message that belong to other users)
        /// </summary>
        /// <param name="todoMessage"></param>
        /// <returns></returns>
        public async Task<bool> RemoveEmoteMessage(MessageIdPayload todoMessage)
        {
            try
            {
                var messageId = todoMessage.MessageId;
                var guild = todoMessage.Guild;

                _logger.LogDebug($"Remove emote message: {messageId}");

                var guildId = guild.Id;

                await _kattbotRepo.RemoveEmotesForMessage(messageId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveEmoteMessage");
                return false;
            }
        }

        /// <summary>
        /// Save emote from reaction
        /// Do not save emote if it does not belong to guild
        /// </summary>
        /// <param name="todoReaction"></param>
        /// <returns></returns>
        public async Task<bool> CreateEmoteReaction(ReactionCommandPayload todoReaction)
        {
            try
            {
                var message = todoReaction.Message;
                var emoji = todoReaction.Emoji;
                var guild = todoReaction.Guild;
                var username = todoReaction.User.Username;
                var userId = todoReaction.User.Id;

                _logger.LogDebug($"Reaction: {username} -> {emoji.Name}");

                if (!IsValidEmote(emoji, guild))
                {
                    _logger.LogDebug($"{emoji.Name} is not valid");
                    return true;
                }

                var emoteEntity = _emoteBuilder.BuildFromUserReaction(message, emoji, userId, guild.Id);

                _logger.LogDebug($"Saving reaction emote {emoteEntity}");

                await _kattbotRepo.CreateEmoteEntity(emoteEntity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateEmoteReaction");
                return false;
            }
        }

        /// <summary>
        /// Delete emote from reaction if it exists
        /// Do not save emote if it does not belong to guild
        /// </summary>
        /// <param name="todoReaction"></param>
        /// <returns></returns>
        public async Task<bool> RemoveEmoteReaction(ReactionCommandPayload todoReaction)
        {
            try
            {
                var emoji = todoReaction.Emoji;
                var guild = todoReaction.Guild;
                var message = todoReaction.Message;
                var username = todoReaction.User.Username;
                var userId = todoReaction.User.Id;

                _logger.LogDebug($"Remove reaction: {username} -> {emoji.Name}");

                if (!IsValidEmote(emoji, guild))
                {
                    _logger.LogDebug($"{emoji.Name} is not valid");
                    return true;
                }

                var emoteEntity = _emoteBuilder.BuildFromUserReaction(message, emoji, userId, guild.Id);

                _logger.LogDebug($"Removing reaction emote {emoteEntity}");

                await _kattbotRepo.RemoveEmoteEntity(emoteEntity);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveEmoteReaction");
                return false;
            }
        }

        /// <summary>
        /// Not emoji, belongs to guild
        /// </summary>
        /// <returns></returns>
        private bool IsValidEmote(DiscordEmoji emoji, DiscordGuild guild)
        {
            return guild.Emojis.ContainsKey(emoji.Id);
        }

        /// <summary>
        /// Not emoji, belongs to guild
        /// </summary>
        /// <returns></returns>
        private bool IsValidEmote(EmoteEntity emote, DiscordGuild guild)
        {
            return guild.Emojis.ContainsKey(emote.EmoteId);
        }
    }
}
