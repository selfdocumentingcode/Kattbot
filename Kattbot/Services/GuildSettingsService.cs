using System;
using System.Threading.Tasks;
using Kattbot.Data;
using Kattbot.Services.Cache;

namespace Kattbot.Services;

public class GuildSettingsService
{
    private static readonly string BotChannel = "BotChannel";
    private static readonly string KattGptChannel = "KattGptChannel";
    private static readonly string KattGptishChannel = "KattGptishChannel";

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

        var cacheKey = SharedCache.BotChannel(guildId);

        _cache.FlushCache(cacheKey);
    }

    public async Task<ulong?> GetBotChannelId(ulong guildId)
    {
        var cacheKey = SharedCache.BotChannel(guildId);

        var channelId = await _cache.LoadFromCacheAsync(
            cacheKey,
            async () => await _guildSettingsRepo.GetGuildSetting(guildId, BotChannel),
            TimeSpan.FromMinutes(60));

        var parsed = ulong.TryParse(channelId, out var result);

        return parsed ? result : null;
    }
}
