using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionResponseErrorWrapper
{
    [JsonPropertyName("error")]
    public ChatCompletionResponseError Error { get; set; } = null!;
}
