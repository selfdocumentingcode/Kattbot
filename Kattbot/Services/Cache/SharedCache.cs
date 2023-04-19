namespace Kattbot.Services.Cache;

public class SharedCache : SimpleMemoryCache
{
    private const int CacheSize = 1024;

    public SharedCache()
        : base(CacheSize)
    {
    }

    public static string BotChannel(ulong guildId) => $"BotChannel_{guildId}";

    public static string KattGptChannel(ulong guildId) => $"KattGptChannel_{guildId}";

    public static string KattGptishChannel(ulong guildId) => $"KattGptishChannel_{guildId}";
}
