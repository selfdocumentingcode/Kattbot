using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using Kattbot.Data.Repositories;
using Kattbot.Helpers;
using Kattbot.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Kattbot.NotificationHandlers.Emotes;

/// <summary>
///     Extract emotes from message text
///     If message contains emotes, save each emote
///     Do save emote if it does not belong to guild.
/// </summary>
public record CreateMessageCommand : EventNotification
{
    public CreateMessageCommand(EventContext ctx, DiscordMessage message)
        : base(ctx)
    {
        Message = message;
    }

    public DiscordMessage Message { get; set; }
}

public class CreateMessageCommandHandler : INotificationHandler<CreateMessageCommand>
{
    private readonly EmoteEntityBuilder _emoteBuilder;
    private readonly EmotesRepository _kattbotRepo;
    private readonly ILogger<CreateMessageCommandHandler> _logger;

    public CreateMessageCommandHandler(
        ILogger<CreateMessageCommandHandler> logger,
        EmoteEntityBuilder emoteBuilder,
        EmotesRepository kattbotRepo)
    {
        _logger = logger;
        _emoteBuilder = emoteBuilder;
        _kattbotRepo = kattbotRepo;
    }

    public async Task Handle(CreateMessageCommand request, CancellationToken cancellationToken)
    {
        EventContext ctx = request.Ctx;

        DiscordMessage message = request.Message;
        DiscordGuild guild = ctx.Guild;
        string messageContent = message.Content;
        string username = message.Author.Username;

        _logger.LogDebug($"Emote message: {username} -> {messageContent}");

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
}
