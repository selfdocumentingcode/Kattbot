using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionCreateRequest
{
    /// <summary>
    ///     Gets or sets Id of the model to use.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-model.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    /// <summary>
    ///     Gets or sets the messages to generate chat completions for, in the chat format.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public ChatCompletionMessage[] Messages { get; set; } = null!;

    /// <summary>
    ///     Gets or sets a list of functions the model may generate JSON inputs for.
    ///     https://platform.openai.com/docs/api-reference/chat/create#functions.
    /// </summary>
    [JsonPropertyName("functions")]
    public ChatCompletionFunction[]? Functions { get; set; }

    /// <summary>
    ///     Gets or sets the mode for controlling the model responds to function calls. none means the model does not call a
    ///     function,
    ///     and responds to the end-user. auto means the model can pick between an end-user or calling a function.
    ///     Specifying a particular function via {"name": "my_function"} forces the model to call that function.
    ///     Defaults to "none" when no functions are present and "auto" if functions are present.
    ///     https://platform.openai.com/docs/api-reference/chat/create#function_call.
    /// </summary>
    [JsonPropertyName("function_call")]
    public string? FunctionCall { get; set; }

    /// <summary>
    ///     Gets or sets what sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more
    ///     random,
    ///     while lower values like 0.2 will make it more focused and deterministic.
    ///     We generally recommend altering this or top_p but not both.
    ///     Defaults to 1.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-temperature.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    /// <summary>
    ///     Gets or sets an alternative to sampling with temperature, called nucleus sampling, where the model considers the
    ///     results of the
    ///     tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are
    ///     considered.
    ///     We generally recommend altering this or temperature but not both.
    ///     Defaults to 1.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-top_p.
    /// </summary>
    [JsonPropertyName("top_p")]
    public float? TopP { get; set; }

    /// <summary>
    ///     Gets or sets how many chat completion choices to generate for each input message.
    ///     Defaults to 1.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-n.
    /// </summary>
    [JsonPropertyName("n")]
    public int? N { get; set; }

    /// <summary>
    ///     Gets or sets up to 4 sequences where the API will stop generating further tokens. The returned text will not
    ///     contain the stop
    ///     sequence. Defaults to null.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-stop.
    /// </summary>
    [JsonPropertyName("stop")]
    public string? Stop { get; set; }

    /// <summary>
    ///     Gets or sets if set, partial message deltas will be sent, like in ChatGPT. Tokens will be sent as data-only
    ///     server-sent events as they become available,
    ///     with the stream terminated by a data: [DONE] message.
    ///     Defaults to false.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-stream.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool? Stream { get; set; }

    /// <summary>
    ///     Gets or sets the maximum number of tokens to generate in the chat completion.
    ///     The total length of input tokens and generated tokens is limited by the model's context length.
    ///     Defaults to inf.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-max_tokens.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    ///     Gets or sets number between -2.0 and 2.0. Positive values penalize new tokens based on whether they appear in the
    ///     text so far,
    ///     increasing the model's likelihood to talk about new topics. Defaults to 0
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-presence_penalty.
    /// </summary>
    [JsonPropertyName("presence_penalty")]
    public float? PresencePenalty { get; set; }

    /// <summary>
    ///     Gets or sets number between -2.0 and 2.0. Positive values penalize new tokens based on their existing frequency in
    ///     the text so far,
    ///     decreasing the model's likelihood to repeat the same line verbatim. Defaults to 0
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-frequency_penalty.
    /// </summary>
    [JsonPropertyName("frequency_penalty")]
    public float? FrequencyPenalty { get; set; }

    /// <summary>
    ///     Gets or sets a unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
    ///     https://platform.openai.com/docs/api-reference/chat/create#chat/create-user.
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; set; }
}
