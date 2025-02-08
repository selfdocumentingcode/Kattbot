using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.Config;
using Microsoft.Extensions.Options;

namespace Kattbot.Services.Speech;

public class SpeechHttpClient
{
    private readonly HttpClient _client;

    public SpeechHttpClient(HttpClient client, IOptions<BotOptions> options)
    {
        _client = client;

        _client.BaseAddress = new Uri("https://api.openai.com/v1/audio/");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Value.OpenAiApiKey}");
    }

    public async Task<MemoryStream> CreateSpeech(
        CreateSpeechRequest request,
        CancellationToken cancellationToken = default)
    {
        var opts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

        HttpResponseMessage? response;

        try
        {
            response = await _client.PostAsJsonAsync("speech", request, opts, cancellationToken);

            Stream responseContentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            var memoryStream = new MemoryStream();

            await responseContentStream.CopyToAsync(memoryStream, cancellationToken);

            memoryStream.Position = 0;

            return memoryStream;
        }

        // catch (HttpRequestException) when (responseContentStream != null)
        // {
        //    var parsedResponse = await JsonSerializer.DeserializeAsync<CreateSpeechResponseErrorWrapper>(responseContentStream)
        //                        ?? throw new Exception("Failed to parse error response");

        // throw new Exception(parsedResponse.Error.Message);
        // }
        catch (HttpRequestException ex)
        {
            throw new Exception($"HTTP {ex.StatusCode}: {ex.Message}");
        }
    }
}
