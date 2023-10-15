using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record Choice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public ChatCompletionMessage Message { get; set; } = null!;

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; } = null!;
}
