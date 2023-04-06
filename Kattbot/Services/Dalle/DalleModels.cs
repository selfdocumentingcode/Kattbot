using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kattbot.Services.KattGpt;

public record CreateImageRequest
{
    /// <summary>
    /// Gets or sets a text description of the desired image(s). The maximum length is 1000 characters.
    /// https://platform.openai.com/docs/api-reference/images/create#images/create-prompt.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of images to generate. Must be between 1 and 10.
    /// Defaults to 1.
    /// https://platform.openai.com/docs/api-reference/images/create#images/create-n.
    /// </summary>
    [JsonPropertyName("n")]
    public int? N { get; set; }

    /// <summary>
    /// Gets or sets the size of the generated images. Must be one of 256x256, 512x512, or 1024x1024.
    /// Defaults to 1024x1024
    /// https://platform.openai.com/docs/api-reference/images/create#images/create-size.
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; } = null!;

    /// <summary>
    /// Gets or sets the format in which the generated images are returned. Must be one of url or b64_json.
    /// Defaults to url
    /// https://platform.openai.com/docs/api-reference/images/create#images/create-response_format.
    /// </summary>
    [JsonPropertyName("response_format")]
    public string? ResponseFormat { get; set; } = null!;

    /// <summary>
    /// Gets or sets a unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
    /// https://platform.openai.com/docs/api-reference/chat/create#chat/create-user.
    /// </summary>
    public string? User { get; set; }
}

public record CreateImageResponse
{
    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("data")]
    public IEnumerable<ImageResponseUrlData> Data { get; set; } = null!;
}

public record ImageResponseUrlData
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}
