using System;

namespace Kattbot.Config;

public record GuildOptions
{
    public ulong Id { get; set; }

    public string Name { get; set; } = null!;

    public ChannelOptions[] CategoryOptions { get; set; } = Array.Empty<ChannelOptions>();

    public ChannelOptions[] ChannelOptions { get; set; } = Array.Empty<ChannelOptions>();
}
