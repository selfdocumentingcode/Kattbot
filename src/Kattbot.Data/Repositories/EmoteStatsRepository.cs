﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kattbot.Common.Models;
using Kattbot.Common.Models.Emotes;
using Microsoft.EntityFrameworkCore;

namespace Kattbot.Data.Repositories;

public class EmoteStatsRepository
{
    private readonly KattbotContext _dbContext;

    public EmoteStatsRepository(KattbotContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResult<EmoteStats>> GetGuildEmoteStats(
        ulong guildId,
        SortDirection direction,
        List<TempEmote> guildEmotes,
        int pageOffset,
        int perPage,
        DateTime? fromDate)
    {
        var emotesResult = new List<EmoteStats>();

        List<EmoteStats> emotes = await _dbContext.Emotes
            .AsQueryable()
            .Where(e => e.GuildId == guildId
                        && (!fromDate.HasValue || e.DateTime > fromDate.Value))
            .GroupBy(
                e => new { e.EmoteId, e.EmoteAnimated },
                (key, value) => new EmoteStats
                {
                    EmoteId = key.EmoteId,
                    IsAnimated = key.EmoteAnimated,
                    Usage = value.Count(),
                })
            .ToListAsync();

        emotesResult.AddRange(emotes);

        IEnumerable<EmoteStats> placeholders = guildEmotes
            .Where(a => !emotes.Select(e => e.EmoteId).Contains(a.Id))
            .Select(e => new EmoteStats
            {
                EmoteId = e.Id,
                IsAnimated = e.Animated,
                Usage = 0,
            });

        emotesResult.AddRange(placeholders);

        emotesResult = direction == SortDirection.Asc
            ? emotesResult.OrderBy(u => u.Usage).ToList()
            : emotesResult.OrderByDescending(u => u.Usage).ToList();

        var totalPages = (int)Math.Ceiling((double)emotesResult.Count / perPage);

        int safePageOffset = Math.Max(val1: 0, Math.Min(pageOffset, totalPages - 1));

        var items = new List<EmoteStats>();

        if (totalPages > 0)
        {
            items = emotes
                .Skip(safePageOffset * perPage)
                .Take(perPage)
                .ToList();
        }

        var result = new PaginatedResult<EmoteStats>
        {
            Items = items,
            PageOffset = safePageOffset,
            PageCount = totalPages,
            TotalCount = emotesResult.Count,
        };

        return result;
    }

    public async Task<PaginatedResult<ExtendedStatsQueryResult>> GetBestEmotesForUser(
        ulong guildId,
        ulong userId,
        int pageOffset,
        int perPage,
        DateTime? fromDate)
    {
        IOrderedQueryable<EmoteStats> mainQuery = _dbContext.Emotes
            .AsQueryable()
            .Where(e => e.GuildId == guildId
                        && e.UserId == userId
                        && (!fromDate.HasValue || e.DateTime > fromDate.Value))
            .GroupBy(
                e => new { e.EmoteId, e.EmoteAnimated },
                (key, value) => new EmoteStats
                {
                    EmoteId = key.EmoteId,
                    IsAnimated = key.EmoteAnimated,
                    Usage = value.Count(),
                })
            .OrderByDescending(u => u.Usage);

        int totalCount = await mainQuery.CountAsync();

        var totalPages = (int)Math.Ceiling((double)totalCount / perPage);

        var items = new List<ExtendedStatsQueryResult>();

        int safePageOffset = Math.Max(val1: 0, Math.Min(pageOffset, totalPages - 1));

        if (totalPages > 0)
        {
            items = await mainQuery
                .Skip(safePageOffset * perPage)
                .Take(perPage)
                .Select(e =>
                    new ExtendedStatsQueryResult
                    {
                        EmoteId = e.EmoteId,
                        Usage = e.Usage,
                        IsAnimated = e.IsAnimated,
                        TotalUsage = _dbContext.Emotes
                            .Count(inner => inner.GuildId == guildId
                                            && inner.EmoteId == e.EmoteId
                                            && (!fromDate.HasValue || inner.DateTime > fromDate.Value)),
                    })
                .ToListAsync();
        }

        var result = new PaginatedResult<ExtendedStatsQueryResult>
        {
            Items = items,
            PageOffset = safePageOffset,
            PageCount = totalPages,
            TotalCount = totalCount,
        };

        return result;
    }

    public async Task<EmoteUsageResult> GetSingleEmoteStats(
        ulong guildId,
        TempEmote emote,
        int userCount,
        DateTime? fromDate)
    {
        ulong emoteId = emote.Id;

        IQueryable<EmoteStats> query = _dbContext.Emotes
            .AsQueryable()
            .Where(e => e.GuildId == guildId
                        && e.EmoteId == emoteId
                        && (!fromDate.HasValue || e.DateTime > fromDate.Value))
            .GroupBy(
                e => new { e.EmoteId, e.EmoteAnimated },
                (key, value) => new EmoteStats
                {
                    EmoteId = key.EmoteId,
                    IsAnimated = key.EmoteAnimated,
                    Usage = value.Count(),
                });

        EmoteStats? emoteStatsResult = await query.SingleOrDefaultAsync();

        IQueryable<EmoteUser> usersQuery = _dbContext.Emotes
            .AsQueryable()
            .Where(e => e.GuildId == guildId
                        && e.EmoteId == emoteId
                        && (!fromDate.HasValue || e.DateTime > fromDate.Value))
            .GroupBy(
                e => new { e.UserId },
                (key, value) => new EmoteUser
                {
                    UserId = key.UserId,
                    Usage = value.Count(),
                })
            .OrderByDescending(e => e.Usage)
            .Take(userCount);

        List<EmoteUser> usersResult = await usersQuery.ToListAsync();

        var result = new EmoteUsageResult
        {
            EmoteStats = emoteStatsResult,
            EmoteUsers = usersResult,
        };

        return result;
    }
}

public enum SortDirection
{
    Asc,
    Desc,
}
