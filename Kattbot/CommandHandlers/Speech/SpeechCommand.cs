using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Kattbot.Services.Speech;
using MediatR;

namespace Kattbot.CommandHandlers.Speech;

public class SpeakTextRequest : CommandRequest
{
    public SpeakTextRequest(CommandContext ctx, string text)
        : base(ctx)
    {
        Text = text;
    }

    public string Text { get; }
}

public class SpeakRequestHandler : IRequestHandler<SpeakTextRequest>
{
    private const int MaxTextLength = 4096;
    private const string AudioFormat = "mp3";
    private const string AudioModel = "tts-1";

    private static readonly string[] AudioVoices = ["alloy", "echo", "fable", "onyx", "nova", "shimmer"];

    private readonly SpeechHttpClient _speechHttpClient;

    public SpeakRequestHandler(SpeechHttpClient speechHttpClient)
    {
        _speechHttpClient = speechHttpClient;
    }

    public async Task Handle(SpeakTextRequest request, CancellationToken cancellationToken)
    {
        CommandContext ctx = request.Ctx;
        string text = request.Text;
        DiscordMessage? reply = ctx.Message.ReferencedMessage;

        if (reply is not null)
        {
            text = reply.Content;
        }

        if (string.IsNullOrEmpty(text))
        {
            throw new Exception("I have nothing to say.");
        }

        DiscordMessage message = await ctx.RespondAsync("Working on it");

        try
        {
            string voice = GetRandomVoice();

            string truncatedText = text.Length > MaxTextLength ? text[..MaxTextLength] : text;

            CreateSpeechRequest speechRequest = BuildRequest(truncatedText, voice);

            MemoryStream audioStream = await _speechHttpClient.CreateSpeech(speechRequest, cancellationToken);

            var fileName = $"{Guid.NewGuid()}.{AudioFormat}";

            var responseBuilder = new DiscordMessageBuilder();

            responseBuilder.AddFile(fileName, audioStream);

            await ctx.RespondAsync(responseBuilder);
        }
        finally
        {
            await message.DeleteAsync();
        }
    }

    private static string GetRandomVoice()
    {
        int random = new System.Random().Next(AudioVoices.Length);

        return AudioVoices[random];
    }

    private static CreateSpeechRequest BuildRequest(string text, string voice)
    {
        var request = new CreateSpeechRequest
        {
            Input = text,
            Voice = voice,
            Model = AudioModel,
            ResponseFormat = AudioFormat,
            Speed = 1,
        };

        return request;
    }
}
