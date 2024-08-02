using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Config;
using Microsoft.Extensions.Options;

namespace Kattbot.Services.KattGpt;

public class ChatGptHttpClient
{
    private readonly HttpClient _client;

    public ChatGptHttpClient(HttpClient client, IOptions<BotOptions> options)
    {
        _client = client;

        _client.BaseAddress = new Uri("https://api.openai.com/v1/chat/");
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Value.OpenAiApiKey}");
    }

    public async Task<ChatCompletionCreateResponse> ChatCompletionCreate(ChatCompletionCreateRequest request)
    {
        var opts = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        Stream? responseContentStream = null;

        try
        {
            HttpResponseMessage response = await _client.PostAsJsonAsync("completions", request, opts);
#if DEBUG
            string stringContent = await response.Content.ReadAsStringAsync();
#endif
            responseContentStream = await response.Content.ReadAsStreamAsync();

            response.EnsureSuccessStatusCode();

            ChatCompletionCreateResponse parsedResponse =
                await JsonSerializer.DeserializeAsync<ChatCompletionCreateResponse>(responseContentStream)
                ?? throw new Exception("Failed to parse response");

            return parsedResponse;
        }
        catch (HttpRequestException) when (responseContentStream != null)
        {
            ChatCompletionResponseErrorWrapper parsedResponse =
                await JsonSerializer.DeserializeAsync<ChatCompletionResponseErrorWrapper>(responseContentStream)
                ?? throw new Exception("Failed to parse error response");

            throw new Exception(parsedResponse.Error.Message);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"HTTP {ex.StatusCode}: {ex.Message}");
        }
    }
}
