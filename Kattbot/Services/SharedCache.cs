using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kattbot.Services
{
    public class SharedCache
    {
        private readonly MemoryCache _cache;

        private static readonly object _lock = new object();

        public SharedCache()
        {
            _cache = new MemoryCache(new MemoryCacheOptions()
            {
                SizeLimit = 1024
            });
        }

        public async Task<T> LoadFromCacheAsync<T>(string key, Func<Task<T>> delegateFunction, TimeSpan duration)
        {
            if (_cache.TryGetValue(key, out T value))
                return value;

            var loadedData = await delegateFunction();

            lock (_lock)
            {
                _cache.Set(key, loadedData, new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = duration,
                    Size = 1
                });

                return loadedData;
            }
        }

        public void FlushCache(string key)
        {
            lock(_lock)
            {
                _cache.Remove(key);
            }            
        }
    }

    public static class SharedCacheKeys
    {
        public static string BotChannel => "BotChannel_%d";
    }
}
