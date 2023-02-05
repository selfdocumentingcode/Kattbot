using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using Kattbot.Common.Models.Emotes;
using Kattbot.Helpers;

namespace Kattbot.Services;

public class EmoteEntityBuilder
{
    private readonly EmoteParser _emoteParser;

    public EmoteEntityBuilder(EmoteParser emoteParser)
    {
        _emoteParser = emoteParser;
    }

    public List<EmoteEntity> BuildFromSocketUserMessage(DiscordMessage message, ulong guildId)
    {
        List<string> emojiStrings = _emoteParser.ExtractEmotesFromMessage(message.Content);

        var emotes = emojiStrings.Select(s =>
        {
            TempEmote? parsedEmote = EmoteHelper.Parse(s);

            if (parsedEmote == null)
            {
                throw new Exception($"{s} is not a valid emote string");
            }

            var emoteEntitiy = new EmoteEntity()
            {
                EmoteId = parsedEmote.Id,
                EmoteName = parsedEmote.Name,
                EmoteAnimated = parsedEmote.Animated,
                DateTime = DateTimeOffset.UtcNow,
                UserId = message.Author.Id,
                MessageId = message.Id,
                GuildId = guildId,
                Source = EmoteSource.Message,
            };

            return emoteEntitiy;
        }).ToList();

        return emotes;
    }

    public EmoteEntity BuildFromUserReaction(DiscordMessage message, DiscordEmoji emote, ulong userId, ulong guildId)
    {
        var emoteEntity = new EmoteEntity()
        {
            EmoteId = emote.Id,
            EmoteName = emote.Name,
            EmoteAnimated = emote.IsAnimated,
            DateTime = DateTimeOffset.UtcNow,
            UserId = userId,
            MessageId = message.Id,
            GuildId = guildId,
            Source = EmoteSource.Reaction,
        };

        return emoteEntity;
    }
}
