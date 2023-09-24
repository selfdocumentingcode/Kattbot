using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionMessage
{
    /// <summary>
    /// Gets or sets can be either “system”, “user”, or “assistant”.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    /// <summary>
    /// Gets or sets the content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;

    public static ChatCompletionMessage AsSystem(string content) => new() { Role = "system", Content = content };

    public static ChatCompletionMessage AsUser(string content) => new() { Role = "user", Content = content };

    public static ChatCompletionMessage AsAssistant(string content) => new() { Role = "assistant", Content = content };
}
