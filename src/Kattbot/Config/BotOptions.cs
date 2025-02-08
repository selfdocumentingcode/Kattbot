namespace Kattbot.Config;

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
