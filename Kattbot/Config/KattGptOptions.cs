using System;

namespace Kattbot.Config;

public record KattGptOptions
{
    public const string OptionsKey = "KattGpt";

    public string[] CoreSystemPrompts { get; set; } = Array.Empty<string>();

    public GuildOptions[] GuildOptions { get; set; } = Array.Empty<GuildOptions>();

    public Template[] Templates { get; set; } = Array.Empty<Template>();
}
