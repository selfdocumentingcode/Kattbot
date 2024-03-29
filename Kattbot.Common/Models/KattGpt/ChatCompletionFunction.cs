using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionFunction
{
    /// <summary>
    ///     Gets or sets the name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with
    ///     a maximum length of 64.
    ///     https://platform.openai.com/docs/api-reference/chat/create#functions-name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    ///     Gets or sets a description of what the function does, used by the model to choose when and how to call the
    ///     function.
    ///     https://platform.openai.com/docs/api-reference/chat/create#functions-description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the parameters the functions accepts, described as a JSON Schema object.
    ///     https://platform.openai.com/docs/api-reference/chat/create#functions-parameters.
    /// </summary>
    [JsonPropertyName("parameters")]
    public JsonObject Parameters { get; set; } = null!;
}
