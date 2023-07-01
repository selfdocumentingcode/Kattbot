using System;

namespace Kattbot;

#pragma warning disable SA1402 // File may only contain a single type
public record BotOptions
{
    public const string OptionsKey = "Kattbot";

    public string CommandPrefix { get; set; } = null!;

    public string AlternateCommandPrefix { get; set; } = null!;

    public string ConnectionString { get; set; } = null!;

    public string BotToken { get; set; } = null!;

    public ulong ErrorLogGuildId { get; set; }

    public ulong ErrorLogChannelId { get; set; }

    public string OpenAiApiKey { get; set; } = null!;
}

public record KattGptOptions
{
    public const string OptionsKey = "KattGpt";

    public string[] CoreSystemPrompts { get; set; } = Array.Empty<string>();

    public GuildOptions[] GuildOptions { get; set; } = Array.Empty<GuildOptions>();

    public Template[] Templates { get; set; } = Array.Empty<Template>();
}

public record GuildOptions
{
    public uint Id { get; set; }

    public string[] SystemPrompts { get; set; } = Array.Empty<string>();

    public ChannelOptions[] ChannelOptions { get; set; } = Array.Empty<ChannelOptions>();

    public ChannelOptions[] CategoryOptions { get; set; } = Array.Empty<ChannelOptions>();
}

public record ChannelOptions
{
    public uint Id { get; set; }

    public bool UseChannelTopic { get; set; }

    public bool AlwaysOn { get; set; }

    public string[] SystemPrompts { get; set; } = Array.Empty<string>();
}

public record Template
{
    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;
}
