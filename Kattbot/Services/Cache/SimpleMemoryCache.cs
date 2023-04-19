using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Kattbot.Services.Cache;

public abstract class SimpleMemoryCache
{
    private readonly MemoryCache _cache;

    public SimpleMemoryCache(int cacheSize)
    {
        _cache = new MemoryCache(new MemoryCacheOptions()
        {
            SizeLimit = cacheSize,
        });
    }

    public async Task<T?> LoadFromCacheAsync<T>(string key, Func<Task<T>> delegateFunction, TimeSpan duration)
    {
        if (_cache.TryGetValue(key, out T? value))
            return value;

        var loadedData = await delegateFunction();

        _cache.Set(key, loadedData, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = duration,
            Size = 1,
        });

        return loadedData;
    }

    public void SetCache<T>(string key, T value, TimeSpan duration)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = duration,
            Size = 1,
        });
    }

    public void SetCache<T>(string key, T value, TimeSpan absoluteDuration, TimeSpan slidingDuration)
    {
        _cache.Set(key, value, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = absoluteDuration,
            SlidingExpiration = slidingDuration,
            Size = 1,
        });
    }

    public T? GetCache<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
            return value;

        return default;
    }

    public void FlushCache(string key)
    {
        _cache.Remove(key);
    }
}
