namespace Kattbot.Config;

public record KattGptOptions
{
    public const string OptionsKey = "KattGpt";

    public string[] AlwaysOnIgnoreMessagePrefixes { get; set; } = [];

    public string[] CoreSystemPrompts { get; set; } = [];

    public GuildOptions[] GuildOptions { get; set; } = [];

    public Template[] Templates { get; set; } = [];
}
