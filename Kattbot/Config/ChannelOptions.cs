using System;

namespace Kattbot.Config;

public record ChannelOptions
{
    public ulong Id { get; set; }

    public string? Topic { get; set; }

    public bool FallbackToChannelTopic { get; set; }

    public bool AlwaysOn { get; set; }

    public string[] SystemPrompts { get; set; } = Array.Empty<string>();
}
