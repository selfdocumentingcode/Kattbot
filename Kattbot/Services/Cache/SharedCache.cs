using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Kattbot.Services.Cache;

#pragma warning disable SA1402 // File may only contain a single type

public abstract class SimpleMemoryCache
{
    private static readonly object Lock = new object();

    private readonly MemoryCache _cache;

    public SimpleMemoryCache(int cacheSize)
    {
        _cache = new MemoryCache(new MemoryCacheOptions()
        {
            SizeLimit = cacheSize,
        });
    }

    public async Task<T> LoadFromCacheAsync<T>(string key, Func<Task<T>> delegateFunction, TimeSpan duration)
    {
        if (_cache.TryGetValue(key, out T value))
            return value;

        var loadedData = await delegateFunction();

        lock (Lock)
        {
            _cache.Set(key, loadedData, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = duration,
                Size = 1,
            });

            return loadedData;
        }
    }

    public void SetCache<T>(string key, T value, TimeSpan duration)
    {
        lock (Lock)
        {
            _cache.Set(key, value, new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = duration,
                Size = 1,
            });
        }
    }

    public T? GetCache<T>(string key)
    {
        if (_cache.TryGetValue(key, out T value))
            return value;

        return default;
    }

    public void FlushCache(string key)
    {
        lock (Lock)
        {
            _cache.Remove(key);
        }
    }
}

public class SharedCache : SimpleMemoryCache
{
    private const int CacheSize = 1024;

    public static string BotChannel => "BotChannel_%d";

    public static string KattGptChannel => "KattGptChannel_%d";

    public SharedCache()
        : base(CacheSize)
    {
    }
}
