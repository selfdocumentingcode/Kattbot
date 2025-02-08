using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionToolCall
{
    /// <summary>
    ///     Gets the ID of the tool call.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;

    /// <summary>
    ///     Gets the type of the tool. Currently, only function is supported.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "function";

    /// <summary>
    ///     Gets the function that the model called.
    /// </summary>
    [JsonPropertyName("function")]
    public ChatCompletionFunctionCall FunctionCall { get; init; } = null!;
}
