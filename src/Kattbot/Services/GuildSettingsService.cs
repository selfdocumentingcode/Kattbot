using System;
using System.Threading.Tasks;
using Kattbot.Data.Repositories;
using Kattbot.Services.Cache;

namespace Kattbot.Services;

public class GuildSettingsService
{
    private static readonly string BotChannel = "BotChannel";
    private readonly SharedCache _cache;

    private readonly GuildSettingsRepository _guildSettingsRepo;

    public GuildSettingsService(
        GuildSettingsRepository guildSettingsRepo,
        SharedCache cache)
    {
        _guildSettingsRepo = guildSettingsRepo;
        _cache = cache;
    }

    public async Task SetBotChannel(ulong guildId, ulong channelId)
    {
        await _guildSettingsRepo.SaveGuildSetting(guildId, BotChannel, channelId.ToString());

        string cacheKey = SharedCache.BotChannel(guildId);

        _cache.FlushCache(cacheKey);
    }

    public async Task<ulong?> GetBotChannelId(ulong guildId)
    {
        string cacheKey = SharedCache.BotChannel(guildId);

        string? channelId = await _cache.LoadFromCacheAsync(
            cacheKey,
            async () => await _guildSettingsRepo.GetGuildSetting(guildId, BotChannel),
            TimeSpan.FromMinutes(60));

        bool parsed = ulong.TryParse(channelId, out ulong result);

        return parsed ? result : null;
    }
}
