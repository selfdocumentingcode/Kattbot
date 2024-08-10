using System;
using Microsoft.Extensions.Caching.Memory;

namespace Kattbot.Services.KattGpt;

public class KattGptChannelCache
{
    private const int CacheSize = 128;

    private const int AbsoluteCacheDurationInDays = 7;
    private const int SlidingCacheDurationInHours = 1;

    private readonly MemoryCache _cache = new(
        new MemoryCacheOptions
        {
            SizeLimit = CacheSize,
        });

    public static string KattGptChannelCacheKey(ulong channelId)
    {
        return $"{nameof(KattGptChannelCache)}_{channelId}";
    }

    public KattGptChannelContext? GetCache(string key)
    {
        return _cache.Get<KattGptChannelContext>(key);
    }

    public void SetCache(string key, KattGptChannelContext value)
    {
        _cache.Set(
            key,
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(AbsoluteCacheDurationInDays),
                SlidingExpiration = TimeSpan.FromHours(SlidingCacheDurationInHours),
                Size = 1,
            });
    }
}
