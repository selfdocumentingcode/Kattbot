using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.Common.Models.Emotes;
using Kattbot.Config;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kattbot.NotificationHandlers.Emotes;

/// <summary>
///     Extract emotes from message text
///     If message contains emotes, save each emote
///     Do save emote if it does not belong to guild.
/// </summary>
public class MessageCreatedNotificationHandler : BaseNotificationHandler,
    INotificationHandler<MessageCreatedNotification>
{
    private readonly EmoteEntityBuilder _emoteBuilder;
    private readonly EmotesRepository _kattbotRepo;
    private readonly IOptions<BotOptions> _botOptions;
    private readonly ILogger<MessageCreatedNotificationHandler> _logger;

    public MessageCreatedNotificationHandler(
        ILogger<MessageCreatedNotificationHandler> logger,
        EmoteEntityBuilder emoteBuilder,
        EmotesRepository kattbotRepo,
        IOptions<BotOptions> botOptions)
    {
        _logger = logger;
        _emoteBuilder = emoteBuilder;
        _kattbotRepo = kattbotRepo;
        _botOptions = botOptions;
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        MessageCreatedEventArgs args = notification.EventArgs;

        DiscordMessage message = args.Message;
        DiscordGuild guild = args.Guild;
        string messageContent = message.Content;
        string username = message.Author?.Username ?? "Unknown";

        _logger.LogDebug($"Emote message: {username} -> {messageContent}");

        if (MessageIsCommand(messageContent, _botOptions.Value))
        {
            return;
        }

        if (!IsRelevantMessage(message))
        {
            _logger.LogDebug("Message is not relevant");
            return;
        }

        ulong guildId = guild.Id;

        List<EmoteEntity> emotes = _emoteBuilder.BuildFromSocketUserMessage(message, guildId);

        if (emotes.Count > 0)
        {
            _logger.LogDebug($"Message contains {emotes.Count} emotes", emotes);

            foreach (EmoteEntity emote in emotes)
            {
                if (!EmoteHelper.IsValidEmote(emote, guild))
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
    }

    private static bool MessageIsCommand(string command, BotOptions options)
    {
        string commandPrefix = options.CommandPrefix;
        string altCommandPrefix = options.AlternateCommandPrefix;

        return command.StartsWith(commandPrefix, StringComparison.OrdinalIgnoreCase)
               || command.StartsWith(altCommandPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
