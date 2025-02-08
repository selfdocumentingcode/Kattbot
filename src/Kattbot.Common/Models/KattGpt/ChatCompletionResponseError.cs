using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionResponseError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("param")]
    public string Param { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
}
