﻿using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Services;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kattbot.CommandHandlers.Images
{
    public class DallePromptCommand : CommandRequest
    {
        public string Prompt { get; set; }

        public DallePromptCommand(CommandContext ctx, string prompt) : base(ctx)
        {
            Prompt = prompt;
        }
    }

    public class DallePromptCommandHandler : AsyncRequestHandler<DallePromptCommand>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ImageService _imageService;

        public DallePromptCommandHandler(IHttpClientFactory httpClientFactory, ImageService imageService)
        {
            _httpClientFactory = httpClientFactory;
            _imageService = imageService;
        }

        protected override async Task Handle(DallePromptCommand request, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient();

            var url = "https://backend.craiyon.com/generate";

            var body = new DalleRequest { Prompt = request.Prompt };

            var json = JsonConvert.SerializeObject(body);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var message = await request.Ctx.RespondAsync("Working on it");

            var response = await client.PostAsync(url, data, cancellationToken);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);

            var searchResponse = JsonConvert.DeserializeObject<DalleResponse>(jsonString);

            if (searchResponse?.Images == null) throw new Exception("Couldn't deserialize response");

            var imageResults = searchResponse.Images
                .Select(Convert.FromBase64String)
                .Select(_imageService.GetImageStream)
                .Select(x => x.Result)
                .ToDictionary(t => $"{Guid.NewGuid()}.{t.FileExtension}", t => t.MemoryStream as Stream);

            var responseBuilder = new DiscordMessageBuilder();

            responseBuilder.WithFiles(imageResults);

            await request.Ctx.RespondAsync(responseBuilder);
        }
    }

    public class DalleResponse
    {
        [JsonProperty("images")]
        public List<string>? Images;

        [JsonProperty("version")]
        public string? Version;
    }
    public class DalleRequest
    {
        [JsonProperty("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }
}
