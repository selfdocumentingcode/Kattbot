using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionCreateResponse
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    [JsonPropertyName("object")]
    public string Object { get; init; } = null!;

    [JsonPropertyName("created")]
    public int Created { get; init; }

    [JsonPropertyName("choices")]
    public List<ChatCompletionChoice> Choices { get; init; } = [];

    [JsonPropertyName("usage")]
    public ChatCompletionUsage Usage { get; init; } = null!;
}
