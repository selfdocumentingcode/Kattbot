using System;
using System.Threading.Tasks;
using Kattbot.Data;
using Kattbot.Services.Cache;

namespace Kattbot.Services;

public class GuildSettingsService
{
    private static readonly string BotChannel = "BotChannel";
    private static readonly string KattGptChannel = "KattGptChannel";

    private readonly GuildSettingsRepository _guildSettingsRepo;
    private readonly SharedCache _cache;

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

        var cacheKey = string.Format(SharedCache.BotChannel, guildId);

        _cache.FlushCache(cacheKey);
    }

    public async Task<ulong?> GetBotChannelId(ulong guildId)
    {
        var cacheKey = string.Format(SharedCache.BotChannel, guildId);

        var channelId = await _cache.LoadFromCacheAsync(
            cacheKey,
            async () => await _guildSettingsRepo.GetGuildSetting(guildId, BotChannel),
            TimeSpan.FromMinutes(10));

        var parsed = ulong.TryParse(channelId, out var result);

        return parsed ? result : null;
    }

    public async Task SetKattGptChannel(ulong guildId, ulong channelId)
    {
        await _guildSettingsRepo.SaveGuildSetting(guildId, KattGptChannel, channelId.ToString());

        var cacheKey = string.Format(SharedCache.KattGptChannel, guildId);

        _cache.FlushCache(cacheKey);
    }

    public async Task<ulong?> GetKattGptChannelId(ulong guildId)
    {
        var cacheKey = string.Format(SharedCache.KattGptChannel, guildId);

        var channelId = await _cache.LoadFromCacheAsync(
            cacheKey,
            async () => await _guildSettingsRepo.GetGuildSetting(guildId, KattGptChannel),
            TimeSpan.FromMinutes(10));

        var parsed = ulong.TryParse(channelId, out var result);

        return parsed ? result : null;
    }
}
