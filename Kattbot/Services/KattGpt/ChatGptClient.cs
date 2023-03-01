using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
        JsonSerializerOptions opts = new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

        var response = await _client.PostAsJsonAsync("completions", request, opts);

        try
        {
            response.EnsureSuccessStatusCode();

            var jsonStream = await response.Content.ReadAsStreamAsync();

            var parsedResponse = (await JsonSerializer.DeserializeAsync<ChatCompletionCreateResponse>(jsonStream))
                                ?? throw new Exception("Failed to parse response");

            return parsedResponse;
        }
        catch (Exception)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            throw new Exception($"HTTP {response.StatusCode}: {errorMessage}");
        }
    }
}
