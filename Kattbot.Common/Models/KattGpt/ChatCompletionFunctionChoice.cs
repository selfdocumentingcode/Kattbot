using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionFunctionChoice
{
    /// <summary>
    ///     The name of the function to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
}
