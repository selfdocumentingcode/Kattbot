using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record FunctionCall
{
    public FunctionCall(string name, string arguments)
    {
        Name = name;
        Arguments = arguments;
    }

    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("arguments")]
    public string Arguments { get; }
}
