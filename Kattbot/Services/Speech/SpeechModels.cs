using System.Text.Json.Serialization;

namespace Kattbot.Services.Speech;

public record CreateSpeechRequest
{
    /// <summary>
    /// Gets or sets one of the available TTS models: tts-1 or tts-1-hd.
    /// https://platform.openai.com/docs/api-reference/audio/createSpeech#audio-createspeech-model.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    /// <summary>
    /// Gets or sets the text to generate audio for. The maximum length is 4096 characters.
    /// https://platform.openai.com/docs/api-reference/audio/createSpeech#audio-createspeech-input.
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = null!;

    /// <summary>
    /// Gets or sets the voice to use when generating the audio. Supported voices are alloy, echo, fable, onyx, nova, and shimmer
    /// https://platform.openai.com/docs/api-reference/audio/createSpeech#audio-createspeech-voice.
    /// </summary>
    [JsonPropertyName("voice")]
    public string? Voice { get; set; } = null;

    /// <summary>
    /// Gets or sets the format to audio in. Supported formats are mp3, opus, aac, flac, wav, and pcm.
    /// Defaults to mp3
    /// https://platform.openai.com/docs/api-reference/audio/createSpeech#audio-createspeech-response_format.
    /// </summary>
    [JsonPropertyName("response_format")]
    public string? ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets the speed of the generated audio. Select a value from 0.25 to 4.0.
    /// Defaults to 1.0
    /// https://platform.openai.com/docs/api-reference/audio/createSpeech#audio-createspeech-speed.
    /// </summary>
    [JsonPropertyName("speed")]
    public int? Speed { get; set; }
}
