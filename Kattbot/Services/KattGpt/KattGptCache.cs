using Kattbot.Services.Cache;

namespace Kattbot.Services.KattGpt;

public class KattGptCache : SimpleMemoryCache
{
    public static string MessageCacheKey(ulong channelId) => $"Message_{channelId}";

    private const int CacheSize = 32;

    public KattGptCache()
        : base(CacheSize)
    {
    }
}
