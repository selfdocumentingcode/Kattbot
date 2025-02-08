namespace Kattbot.Services.Cache;

public class SharedCache : SimpleMemoryCache
{
    private const int CacheSize = 1024;

    public SharedCache()
        : base(CacheSize)
    { }

    public static string BotChannel(ulong guildId)
    {
        return $"BotChannel_{guildId}";
    }

    public static string KattGptChannel(ulong guildId)
    {
        return $"KattGptChannel_{guildId}";
    }

    public static string KattGptishChannel(ulong guildId)
    {
        return $"KattGptishChannel_{guildId}";
    }
}
