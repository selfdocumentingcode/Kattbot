using System;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace Kattbot.Services.KattGpt;

public class KattGptChannelCache
{
    private const int CacheSize = 128;

    private const int AbsoluteCacheDurationInDays = 7;
    private const int SlidingCacheDurationInHours = 1;

    private readonly MemoryCache _cache;

    public KattGptChannelCache()
    {
        _cache = new MemoryCache(new MemoryCacheOptions()
        {
            SizeLimit = CacheSize,
        });
    }

    public static string KattGptChannelCacheKey(ulong channelId) => $"{nameof(KattGptChannelCache)}_{channelId}";

    public BoundedQueue<ChatCompletionMessage>? GetCache(string key)
    {
        return _cache.Get<BoundedQueue<ChatCompletionMessage>>(key);
    }

    public void SetCache(string key, BoundedQueue<ChatCompletionMessage> value)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(AbsoluteCacheDurationInDays),
            SlidingExpiration = TimeSpan.FromHours(SlidingCacheDurationInHours),
            Size = 1,
        });
    }
}
