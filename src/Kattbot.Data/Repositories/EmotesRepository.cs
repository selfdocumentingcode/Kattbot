using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kattbot.Common.Models.Emotes;
using Microsoft.EntityFrameworkCore;

namespace Kattbot.Data.Repositories;

public class EmotesRepository
{
    private readonly KattbotContext _dbContext;

    public EmotesRepository(KattbotContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmoteEntity> CreateEmoteEntity(EmoteEntity emote)
    {
        emote.Id = Guid.NewGuid();

        _dbContext.Add(emote);

        await _dbContext.SaveChangesAsync();

        return emote;
    }

    public async Task RemoveEmoteEntity(EmoteEntity emote)
    {
        EmoteEntity? existingEntity = await GetExistingEntity(emote);

        if (existingEntity == null)
        {
            return;
        }

        _dbContext.Remove(existingEntity);

        await _dbContext.SaveChangesAsync();
    }

    private Task<EmoteEntity?> GetExistingEntity(EmoteEntity entity)
    {
        Task<EmoteEntity?> emote = _dbContext.Emotes.AsQueryable()
            .Where(e => e.EmoteId == entity.EmoteId
                        && e.MessageId == entity.MessageId
                        && e.UserId == entity.UserId
                        && e.GuildId == entity.GuildId)
            .FirstOrDefaultAsync();

        return emote;
    }

    public async Task RemoveEmotesForMessage(ulong messageId)
    {
        List<EmoteEntity> emotesForMessage = await _dbContext.Emotes
            .AsQueryable()
            .Where(e => e.MessageId == messageId)

            // .Where(e => e.Source == EmoteSource.Message)
            .ToListAsync();

        _dbContext.RemoveRange(emotesForMessage);

        await _dbContext.SaveChangesAsync();
    }

    // public async Task RemoveUserReactionEmotesForMessage(ulong messageId, ulong userId)
    // {
    //    var emotesForReaction = await _dbContext.Emotes
    //                            .AsQueryable()
    //                            .Where(e => e.MessageId == messageId)
    //                            .Where(e => e.UserId == userId)
    //                            .Where(e => e.Source == EmoteSource.Reaction)
    //                            .ToListAsync();

    // _dbContext.RemoveRange(emotesForReaction);

    // await _dbContext.SaveChangesAsync();
    // }
}
