using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kattbot.Services.Dalle;

#pragma warning disable SA1402 // File may only contain a single type
public record CreateImageRequest
{
    /// <summary>
    ///     Gets or sets a text description of the desired image(s). The maximum length is 1000 characters.
    ///     https://platform.openai.com/docs/api-reference/images/create#images/create-prompt.
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the model to use for image generation.
    ///     Defaults to dall-e-2
    ///     https://platform.openai.com/docs/api-reference/images/create#images-create-model.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    /// <summary>
    ///     Gets or sets the number of images to generate. Must be between 1 and 10.
    ///     Defaults to 1.
    ///     https://platform.openai.com/docs/api-reference/images/create#images/create-n.
    /// </summary>
    [JsonPropertyName("n")]
    public int? N { get; set; }

    /// <summary>
    ///     Gets or sets the quality of the image that will be generated.
    ///     hd creates images with finer details and greater consistency across the image.
    ///     This param is only supported for dall-e-3.
    ///     Defaults to standard
    ///     https://platform.openai.com/docs/api-reference/images/create#images-create-quality.
    /// </summary>
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    /// <summary>
    ///     Gets or sets the size of the generated images.
    ///     Must be one of 256x256, 512x512, or 1024x1024 for dall-e-2.
    ///     Must be one of 1024x1024, 1792x1024, or 1024x1792 for dall-e-3 models.
    ///     Defaults to 1024x1024
    ///     https://platform.openai.com/docs/api-reference/images/create#images/create-size.
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    /// <summary>
    ///     Gets or sets the format in which the generated images are returned. Must be one of url or b64_json.
    ///     Defaults to url
    ///     https://platform.openai.com/docs/api-reference/images/create#images/create-response_format.
    /// </summary>
    [JsonPropertyName("response_format")]
    public string? ResponseFormat { get; set; }

    /// <summary>
    ///     Gets or sets the style of the generated images. Must be one of vivid or natural.
    ///     Vivid causes the model to lean towards generating hyper-real and dramatic images.
    ///     Natural causes the model to produce more natural, less hyper-real looking images.
    ///     This param is only supported for dall-e-3.
    ///     Defaults to vivid
    ///     https://platform.openai.com/docs/api-reference/images/create#images-create-style.
    /// </summary>
    [JsonPropertyName("style")]
    public string? Style { get; set; }

    /// <summary>
    ///     Gets or sets a unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-user.
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }
}

public record CreateImageVariationRequest
{
    /// <summary>
    ///     Gets or sets the image to use as the basis for the variation(s). Must be a valid PNG file, less than 4MB, and
    ///     square.
    ///     https://platform.openai.com/docs/api-reference/images/createVariation#image.
    /// </summary>
    [JsonPropertyName("image")]
    public byte[] Image { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the number of images to generate. Must be between 1 and 10.
    ///     Defaults to 1.
    ///     https://platform.openai.com/docs/api-reference/images/createVariation#n.
    /// </summary>
    [JsonPropertyName("n")]
    public int? N { get; set; }

    /// <summary>
    ///     Gets or sets the size of the generated images. Must be one of 256x256, 512x512, or 1024x1024.
    ///     Defaults to 1024x1024
    ///     https://platform.openai.com/docs/api-reference/images/createVariation#size.
    /// </summary>
    [JsonPropertyName("size")]
    public string? Size { get; set; }

    /// <summary>
    ///     Gets or sets the format in which the generated images are returned. Must be one of url or b64_json.
    ///     Defaults to url
    ///     https://platform.openai.com/docs/api-reference/images/createVariation#response_format.
    /// </summary>
    [JsonPropertyName("response_format")]
    public string? ResponseFormat { get; set; }

    /// <summary>
    ///     Gets or sets a unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
    ///     https://platform.openai.com/docs/api-reference/images/createVariation#user.
    /// </summary>
    [JsonPropertyName("user")]
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
