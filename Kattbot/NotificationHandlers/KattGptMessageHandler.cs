using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Config;
using Kattbot.Helpers;
using Kattbot.Services;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using Kattbot.Services.KattGpt;
using MediatR;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Kattbot.NotificationHandlers;

public class KattGptMessageHandler : BaseNotificationHandler,
    INotificationHandler<MessageCreatedNotification>
{
    private const string ChatGptModel = "gpt-4o";
    private const string TokenizerModel = "gpt-4o";
    private const string CreateImageModel = "dall-e-3";
    private const float DefaultTemperature = 1.2f;
    private const float FunctionCallTemperature = 0.8f;
    private const int MaxTotalTokens = 24_576;
    private const int MaxTokensToGenerate = 960; // Roughly the limit of 2 Discord messages
    private const string MessageSplitToken = "[cont.] ";
    private const string RecipientMarkerToYou = "[to you]";
    private const string RecipientMarkerToOthers = "[to others]";
    private readonly KattGptChannelCache _cache;

    private readonly ChatGptHttpClient _chatGpt;
    private readonly DalleHttpClient _dalleHttpClient;
    private readonly DiscordErrorLogger _discordErrorLogger;
    private readonly ImageService _imageService;
    private readonly KattGptOptions _kattGptOptions;
    private readonly KattGptService _kattGptService;

    public KattGptMessageHandler(
        ChatGptHttpClient chatGpt,
        KattGptChannelCache cache,
        KattGptService kattGptService,
        DalleHttpClient dalleHttpClient,
        ImageService imageService,
        DiscordErrorLogger discordErrorLogger,
        IOptions<KattGptOptions> kattGptOptions)
    {
        _chatGpt = chatGpt;
        _cache = cache;
        _kattGptService = kattGptService;
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
        _discordErrorLogger = discordErrorLogger;
        _kattGptOptions = kattGptOptions.Value;
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        MessageCreatedEventArgs args = notification.EventArgs;
        DiscordMessage message = args.Message;
        DiscordUser author = args.Author;
        DiscordChannel? channel = args.Message.Channel;

        if (!ShouldHandleMessage(message)) return;

        var kattGptTokenizer = new KattGptTokenizer(TokenizerModel);

        List<ChatCompletionMessage> systemPromptsMessages = _kattGptService.BuildSystemPromptsMessages(channel);

        int systemMessagesTokenCount = kattGptTokenizer.GetTokenCount(systemPromptsMessages);

        int functionsTokenCount =
            kattGptTokenizer.GetTokenCount(DalleFunctionBuilder.BuildDalleImageFunctionDefinition());

        int reservedTokens = systemMessagesTokenCount + functionsTokenCount;

        BoundedQueue<ChatCompletionMessage> boundedMessageQueue = GetBoundedMessageQueue(channel, reservedTokens);

        // Add new message from notification
        string newMessageUser = author.GetDisplayName();
        string newMessageContent = message.SubstituteMentions();

        bool shouldReplyToMessage = ShouldReplyToMessage(message);

        string recipientMarker = shouldReplyToMessage
            ? RecipientMarkerToYou
            : RecipientMarkerToOthers;

        ChatCompletionMessage newUserMessage =
            ChatCompletionMessage.AsUser($"{newMessageUser}{recipientMarker}: {newMessageContent}");

        boundedMessageQueue.Enqueue(newUserMessage, kattGptTokenizer.GetTokenCount(newUserMessage.Content));

        if (shouldReplyToMessage)
        {
            await channel.TriggerTypingAsync();

            ChatCompletionCreateRequest request = BuildRequest(
                systemPromptsMessages,
                boundedMessageQueue,
                DefaultTemperature);

            ChatCompletionCreateResponse response = await _chatGpt.ChatCompletionCreate(request);

            ChatCompletionMessage chatGptResponse = response.Choices[0].Message;

            if (chatGptResponse.FunctionCall != null)
            {
                await HandleFunctionCallResponse(
                    message,
                    kattGptTokenizer,
                    systemPromptsMessages,
                    boundedMessageQueue,
                    chatGptResponse);
            }
            else
            {
                await SendReply(chatGptResponse.Content!, message);

                // Add the chat gpt response message to the bounded queue
                boundedMessageQueue.Enqueue(chatGptResponse, kattGptTokenizer.GetTokenCount(chatGptResponse.Content));
            }
        }

        SaveBoundedMessageQueue(channel, boundedMessageQueue);
    }

    private static ChatCompletionCreateRequest BuildRequest(
        List<ChatCompletionMessage> systemPromptsMessages,
        BoundedQueue<ChatCompletionMessage> boundedMessageQueue,
        float temperature)
    {
        // Build functions
        ChatCompletionFunction[] chatCompletionFunctions =
        {
            DalleFunctionBuilder.BuildDalleImageFunctionDefinition(),
        };

        // Collect request messages
        var requestMessages = new List<ChatCompletionMessage>();
        requestMessages.AddRange(systemPromptsMessages);
        requestMessages.AddRange(boundedMessageQueue.GetAll());

        // Make request
        var request = new ChatCompletionCreateRequest
        {
            Model = ChatGptModel,
            Messages = requestMessages.ToArray(),
            Temperature = temperature,
            MaxTokens = MaxTokensToGenerate,
            Functions = chatCompletionFunctions,
        };

        return request;
    }

    private static async Task SendDalleResultReply(
        string responseMessage,
        DiscordMessage messageToReplyTo,
        string prompt,
        ImageStreamResult imageStream)
    {
        string truncatedPrompt = prompt.Length > DiscordConstants.MaxEmbedTitleLength
            ? $"{prompt[..(DiscordConstants.MaxEmbedTitleLength - 3)]}..."
            : prompt;

        string filename = prompt.ToSafeFilename(imageStream.FileExtension);

        DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
            .WithTitle(truncatedPrompt)
            .WithImageUrl($"attachment://{filename}");

        DiscordMessageBuilder mb = new DiscordMessageBuilder()
            .AddFile(filename, imageStream.MemoryStream)
            .AddEmbed(eb)
            .WithContent(responseMessage);

        await messageToReplyTo.RespondAsync(mb);
    }

    private static async Task SendReply(string responseMessage, DiscordMessage messageToReplyTo)
    {
        List<string> messageChunks = responseMessage.SplitString(DiscordConstants.MaxMessageLength, MessageSplitToken);

        DiscordMessage nextMessageToReplyTo = messageToReplyTo;

        foreach (string messageChunk in messageChunks)
        {
            nextMessageToReplyTo = await nextMessageToReplyTo.RespondAsync(messageChunk);
        }
    }

    private async Task<ImageStreamResult> GetDalleResult(string prompt, string userId)
    {
        var imageRequest = new CreateImageRequest
        {
            Prompt = prompt,
            Model = CreateImageModel,
            User = userId,
        };

        CreateImageResponse response = await _dalleHttpClient.CreateImage(imageRequest);
        if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

        ImageResponseUrlData imageUrl = response.Data.First();

        Image image = await _imageService.DownloadImage(imageUrl.Url);

        ImageStreamResult imageStream = await _imageService.GetImageStream(image);

        return imageStream;
    }

    private async Task HandleFunctionCallResponse(
        DiscordMessage message,
        KattGptTokenizer kattGptTokenizer,
        List<ChatCompletionMessage> systemPromptsMessages,
        BoundedQueue<ChatCompletionMessage> boundedMessageQueue,
        ChatCompletionMessage chatGptResponse)
    {
        DiscordMessage? workingOnItMessage = null;

        try
        {
            ulong authorId = message.Author.Id;

            // Force a content value for the chat gpt response due the api not allowing nulls even though it says it does
            chatGptResponse.Content ??= "null";

            // Parse and execute function call
            string functionCallName = chatGptResponse.FunctionCall!.Name;
            string functionCallArguments = chatGptResponse.FunctionCall.Arguments;

            JsonNode parsedArguments = JsonNode.Parse(functionCallArguments)
                                       ?? throw new Exception("Could not parse function call arguments.");

            string prompt = parsedArguments["prompt"]?.GetValue<string>()
                            ?? throw new Exception("Function call arguments are invalid.");

            workingOnItMessage = await message.RespondAsync($"Kattbot used: {prompt}");

            ImageStreamResult dalleResult = await GetDalleResult(prompt, authorId.ToString());

            // Send request with function result
            var functionCallResult = "The resulting image file will be attached to your next message.";

            ChatCompletionMessage functionCallResultMessage =
                ChatCompletionMessage.AsFunctionCallResult(functionCallName, functionCallResult);
            int functionCallResultTokenCount =
                kattGptTokenizer.GetTokenCount(functionCallName, functionCallArguments, functionCallResult);

            // Add chat gpt response to the context
            int chatGptResponseTokenCount = kattGptTokenizer.GetTokenCount(chatGptResponse.Content);
            boundedMessageQueue.Enqueue(chatGptResponse, chatGptResponseTokenCount);

            // Add function call result to the context
            boundedMessageQueue.Enqueue(functionCallResultMessage, functionCallResultTokenCount);

            ChatCompletionCreateRequest request = BuildRequest(
                systemPromptsMessages,
                boundedMessageQueue,
                FunctionCallTemperature);

            ChatCompletionCreateResponse response = await _chatGpt.ChatCompletionCreate(request);

            // Handle new response
            ChatCompletionMessage functionCallResponse = response.Choices[0].Message;
            boundedMessageQueue.Enqueue(
                functionCallResponse,
                kattGptTokenizer.GetTokenCount(functionCallResponse.Content));

            await workingOnItMessage.DeleteAsync();

            await SendDalleResultReply(functionCallResponse.Content!, message, prompt, dalleResult);
        }
        catch (Exception ex)
        {
            if (workingOnItMessage is not null) await workingOnItMessage.DeleteAsync();

            await SendReply("Something went wrong", message);
            _discordErrorLogger.LogError(ex.Message);
        }
    }

    /// <summary>
    ///     Gets the bounded message queue for the channel from the cache or creates a new one.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <param name="reservedTokenCount">The token count for the system messages and functions.</param>
    /// <returns>The bounded message queue for the channel.</returns>
    private BoundedQueue<ChatCompletionMessage> GetBoundedMessageQueue(DiscordChannel channel, int reservedTokenCount)
    {
        string cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);

        BoundedQueue<ChatCompletionMessage>? boundedMessageQueue = _cache.GetCache(cacheKey);

        if (boundedMessageQueue == null)
        {
            int remainingTokensForContextMessages = MaxTotalTokens - MaxTokensToGenerate - reservedTokenCount;

            boundedMessageQueue = new BoundedQueue<ChatCompletionMessage>(remainingTokensForContextMessages);
        }

        return boundedMessageQueue;
    }

    /// <summary>
    ///     Saves the bounded message queue for the channel to the cache.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <param name="boundedMessageQueue">The bounded message queue.</param>
    private void SaveBoundedMessageQueue(
        DiscordChannel channel,
        BoundedQueue<ChatCompletionMessage> boundedMessageQueue)
    {
        string cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);
        _cache.SetCache(cacheKey, boundedMessageQueue);
    }

    /// <summary>
    ///     Checks if the message should be handled by Kattgpt.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>True if the message should be handled by Kattgpt.</returns>
    private bool ShouldHandleMessage(DiscordMessage message)
    {
        if (!IsRelevantMessage(message)) return false;

        DiscordChannel? channel = message.Channel;

        ChannelOptions? channelOptions = _kattGptService.GetChannelOptions(channel);

        if (channelOptions == null) return false;

        // if the channel is not always on, handle the message
        if (!channelOptions.AlwaysOn) return true;

        // otherwise check if the message does not start with the MetaMessagePrefix
        string[] metaMessagePrefixes = _kattGptOptions.AlwaysOnIgnoreMessagePrefixes;
        bool messageStartsWithMetaMessagePrefix = metaMessagePrefixes.Any(message.Content.TrimStart().StartsWith);

        // if it does, return false
        return !messageStartsWithMetaMessagePrefix;
    }

    /// <summary>
    ///     Checks if Kattgpt should reply to the message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>True if Kattgpt should reply.</returns>
    private bool ShouldReplyToMessage(DiscordMessage message)
    {
        DiscordChannel? channel = message.Channel;

        ChannelOptions? channelOptions = _kattGptService.GetChannelOptions(channel);

        if (channelOptions == null) return false;

        // if the channel is not always on
        if (!channelOptions.AlwaysOn)
        {
            // check if the current message is a reply to kattbot
            bool messageIsReplyToKattbot = message.ReferencedMessage?.Author?.IsCurrent ?? false;

            if (messageIsReplyToKattbot) return true;

            // or if kattbot is mentioned
            bool kattbotIsMentioned = message.MentionedUsers.Any(u => u.IsCurrent);

            return kattbotIsMentioned;
        }

        // otherwise check if the message does not start with the MetaMessagePrefix
        string[] metaMessagePrefixes = _kattGptOptions.AlwaysOnIgnoreMessagePrefixes;
        bool messageStartsWithMetaMessagePrefix = metaMessagePrefixes.Any(message.Content.TrimStart().StartsWith);

        // if it does, return false
        return !messageStartsWithMetaMessagePrefix;
    }
}
