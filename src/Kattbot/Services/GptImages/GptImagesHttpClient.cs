using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Config;
using Microsoft.Extensions.Options;

namespace Kattbot.Services.GptImages;

public class GptImagesHttpClient
{
    private readonly HttpClient _client;

    public GptImagesHttpClient(HttpClient client, IOptions<BotOptions> options)
    {
        _client = client;

        _client.BaseAddress = new Uri("https://api.openai.com/v1/images/");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Value.OpenAiApiKey}");
    }

    public async Task<CreateImageResponse> CreateImage(
        CreateImageRequest request,
        CancellationToken cancellationToken = default)
    {
        var opts = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };

        Stream? responseContentStream = null;

        try
        {
            HttpResponseMessage response = await _client.PostAsJsonAsync(
                "generations",
                request,
                opts,
                cancellationToken);

            responseContentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            CreateImageResponse parsedResponse =
                await JsonSerializer.DeserializeAsync<CreateImageResponse>(
                    responseContentStream,
                    cancellationToken: cancellationToken)
                ?? throw new Exception("Failed to parse response");

            return parsedResponse;
        }
        catch (HttpRequestException) when (responseContentStream != null)
        {
            ChatCompletionResponseErrorWrapper parsedResponse =
                await JsonSerializer.DeserializeAsync<ChatCompletionResponseErrorWrapper>(
                    responseContentStream,
                    cancellationToken: cancellationToken)
                ?? throw new Exception("Failed to parse error response");

            throw new Exception(parsedResponse.Error.Message);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"HTTP {ex.StatusCode}: {ex.Message}");
        }
    }

    public async Task<CreateImageResponse> CreateImageEdit(
        CreateImageEditRequest request,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var postBody = new MultipartFormDataContent();

        postBody.Add(new StringContent(request.Prompt), "prompt");

        if (request.Model != null)
        {
            postBody.Add(new StringContent(request.Model), "model");
        }

        if (request.N.HasValue)
        {
            postBody.Add(new StringContent(request.N.ToString()!), "n");
        }

        if (request.Quality != null)
        {
            postBody.Add(new StringContent(request.Quality), "quality");
        }

        if (request.Size != null)
        {
            postBody.Add(new StringContent(request.Size), "size");
        }

        if (request.User != null)
        {
            postBody.Add(new StringContent(request.User), "user");
        }

        postBody.Add(new ByteArrayContent(request.Image), "image", fileName);

        Stream? responseContentStream = null;

        try
        {
            HttpResponseMessage response = await _client.PostAsync("edits", postBody, cancellationToken);

            responseContentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            response.EnsureSuccessStatusCode();

            CreateImageResponse parsedResponse =
                await JsonSerializer.DeserializeAsync<CreateImageResponse>(
                    responseContentStream,
                    cancellationToken: cancellationToken)
                ?? throw new Exception("Failed to parse response");

            return parsedResponse;
        }
        catch (HttpRequestException) when (responseContentStream != null)
        {
            ChatCompletionResponseErrorWrapper parsedResponse =
                await JsonSerializer.DeserializeAsync<ChatCompletionResponseErrorWrapper>(
                    responseContentStream,
                    cancellationToken: cancellationToken)
                ?? throw new Exception("Failed to parse error response");

            throw new Exception(parsedResponse.Error.Message);
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"HTTP {ex.StatusCode}: {ex.Message}");
        }
    }
}
