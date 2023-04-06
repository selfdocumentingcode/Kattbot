using System;
using Kattbot.Services.Cache;

namespace Kattbot.Services.KattGpt;

public class KattGptMessageCacheQueue : CacheQueue<ChatCompletionMessage>
{
    private const int MaxSize = 32;
    private const int MaxAgeMinutes = 5;

    public KattGptMessageCacheQueue()
        : base(MaxSize, TimeSpan.FromMinutes(MaxAgeMinutes))
    {
    }
}
