using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Helpers;
using Kattbot.Services;
using Kattbot.Services.Dalle;
using Kattbot.Services.Images;
using Kattbot.Services.KattGpt;
using MediatR;

namespace Kattbot.NotificationHandlers;

public class KattGptMessageHandler : INotificationHandler<MessageCreatedNotification>
{
    private const string ChatGptModel = "gpt-3.5-turbo-16k";
    private const string TokenizerModel = "gpt-3.5";
    private const string MetaMessagePrefix = "$";
    private const float Temperature = 1.2f;
    private const int MaxTokens = 8192;
    private const int MaxTokensToGenerate = 960; // Roughly the limit of 2 Discord messages
    private const string MessageSplitToken = "[cont.]";

    private readonly ChatGptHttpClient _chatGpt;
    private readonly KattGptChannelCache _cache;
    private readonly KattGptService _kattGptService;
    private readonly DalleHttpClient _dalleHttpClient;
    private readonly ImageService _imageService;
    private readonly DiscordErrorLogger _discordErrorLogger;

    public KattGptMessageHandler(
        ChatGptHttpClient chatGpt,
        KattGptChannelCache cache,
        KattGptService kattGptService,
        DalleHttpClient dalleHttpClient,
        ImageService imageService,
        DiscordErrorLogger discordErrorLogger)
    {
        _chatGpt = chatGpt;
        _cache = cache;
        _kattGptService = kattGptService;
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
        _discordErrorLogger = discordErrorLogger;
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        var args = notification.EventArgs;
        var message = args.Message;
        var author = args.Author;
        var channel = args.Message.Channel;

        if (!ShouldHandleMessage(message))
        {
            return;
        }

        var kattGptTokenizer = new KattGptTokenizer(TokenizerModel);

        var systemPromptsMessages = _kattGptService.BuildSystemPromptsMessages(channel);

        var systemMessagesTokenCount = kattGptTokenizer.GetTokenCount(systemPromptsMessages);

        var boundedMessageQueue = GetBoundedMessageQueue(channel, systemMessagesTokenCount);

        // Add new message from notification
        var newMessageUser = author.GetDisplayName();
        var newMessageContent = message.GetMessageWithTextMentions();

        var newUserMessage = ChatCompletionMessage.AsUser($"{newMessageUser}: {newMessageContent}");

        boundedMessageQueue.Enqueue(newUserMessage, kattGptTokenizer.GetTokenCount(newUserMessage.Content));

        if (ShouldReplyToMessage(message))
        {
            await channel.TriggerTypingAsync();

            var request = BuildRequest(systemPromptsMessages, boundedMessageQueue);

            var response = await _chatGpt.ChatCompletionCreate(request);

            var chatGptResponse = response.Choices[0].Message;

            if (chatGptResponse.FunctionCall != null)
            {
                await HandleFunctionCallResponse(message, kattGptTokenizer, systemPromptsMessages, boundedMessageQueue, chatGptResponse);
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

    private static ChatCompletionCreateRequest BuildRequest(List<ChatCompletionMessage> systemPromptsMessages, BoundedQueue<ChatCompletionMessage> boundedMessageQueue)
    {
        // Build functions
        var chatCompletionFunctions = new[] { DalleFunctionBuilder.BuildDalleImageFunctionDefinition() };

        // Collect request messages
        var requestMessages = new List<ChatCompletionMessage>();
        requestMessages.AddRange(systemPromptsMessages);
        requestMessages.AddRange(boundedMessageQueue.GetAll());

        // Make request
        var request = new ChatCompletionCreateRequest()
        {
            Model = ChatGptModel,
            Messages = requestMessages.ToArray(),
            Temperature = Temperature,
            MaxTokens = MaxTokensToGenerate,
            Functions = chatCompletionFunctions,
        };

        return request;
    }

    private static async Task SendDalleResultReply(string responseMessage, DiscordMessage messageToReplyTo, string prompt, ImageStreamResult imageStream)
    {
        var truncatedPrompt = prompt.Length > DiscordConstants.MaxEmbedTitleLength
            ? $"{prompt[..(DiscordConstants.MaxEmbedTitleLength - 3)]}..."
            : prompt;

        var filename = prompt.ToSafeFilename(imageStream.FileExtension);

        DiscordEmbedBuilder eb = new DiscordEmbedBuilder()
            .WithTitle(truncatedPrompt)
            .WithImageUrl($"attachment://{filename}");

        DiscordMessageBuilder mb = new DiscordMessageBuilder()
            .AddFile(filename, imageStream.MemoryStream)
            .WithEmbed(eb)
            .WithContent(responseMessage);

        await messageToReplyTo.RespondAsync(mb);
    }

    private static async Task SendReply(string responseMessage, DiscordMessage messageToReplyTo)
    {
        var messageChunks = responseMessage.SplitString(DiscordConstants.MaxMessageLength, MessageSplitToken);

        var nextMessageToReplyTo = messageToReplyTo;

        foreach (var messageChunk in messageChunks)
        {
            nextMessageToReplyTo = await nextMessageToReplyTo.RespondAsync(messageChunk);
        }
    }

    private async Task<ImageStreamResult> GetDalleResult(string prompt, string userId)
    {
        var response = await _dalleHttpClient.CreateImage(new CreateImageRequest { Prompt = prompt, User = userId });
        if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

        var imageUrl = response.Data.First();

        var image = await _imageService.DownloadImage(imageUrl.Url);

        var imageStream = await _imageService.GetImageStream(image);

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
            var authorId = message.Author.Id;

            // Force a content value for the chat gpt response due the api not allowing nulls even though it says it does
            chatGptResponse.Content ??= "null";

            // Parse and execute function call
            var functionCallName = chatGptResponse.FunctionCall!.Name;
            var functionCallArguments = chatGptResponse.FunctionCall.Arguments;

            var parsedArguments = JsonNode.Parse(functionCallArguments)
                ?? throw new Exception("Could not parse function call arguments.");

            var prompt = parsedArguments["prompt"]?.GetValue<string>()
                ?? throw new Exception("Function call arguments are invalid.");

            workingOnItMessage = await message.RespondAsync($"Kattbot used: {prompt}");

            var dalleResult = await GetDalleResult(prompt, authorId.ToString());

            // Send request with function result
            var functionCallResult = $"The prompt itself and the image result are attached to this post.";

            var functionCallResultMessage = ChatCompletionMessage.AsFunctionCallResult(functionCallName, functionCallResult);
            var functionCallResultTokenCount = kattGptTokenizer.GetTokenCount(functionCallName, functionCallArguments, functionCallResult);

            // Add chat gpt response to the context
            var chatGptResponseTokenCount = kattGptTokenizer.GetTokenCount(chatGptResponse.Content);
            boundedMessageQueue.Enqueue(chatGptResponse, chatGptResponseTokenCount);

            // Add function call result to the context
            boundedMessageQueue.Enqueue(functionCallResultMessage, functionCallResultTokenCount);

            var request2 = BuildRequest(systemPromptsMessages, boundedMessageQueue);

            var response2 = await _chatGpt.ChatCompletionCreate(request2);

            // Handle new response
            var chatGptResponse2 = response2.Choices[0].Message;

            await workingOnItMessage.DeleteAsync();

            await SendDalleResultReply(chatGptResponse2.Content!, message, prompt, dalleResult);
        }
        catch (Exception ex)
        {
            if (workingOnItMessage is not null)
            {
                await workingOnItMessage.DeleteAsync();
            }

            await SendReply("Something went wrong", message);
            _discordErrorLogger.LogError(ex.Message);
        }
    }

    /// <summary>
    /// Gets the bounded message queue for the channel from the cache or creates a new one.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <param name="systemMessageTokenCount">The token count for the system messages.</param>
    /// <returns>The bounded message queue for the channel.</returns>
    private BoundedQueue<ChatCompletionMessage> GetBoundedMessageQueue(DiscordChannel channel, int systemMessageTokenCount)
    {
        var cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);

        var boundedMessageQueue = _cache.GetCache(cacheKey);

        if (boundedMessageQueue == null)
        {
            var remainingTokensForContextMessages = MaxTokens - systemMessageTokenCount;

            boundedMessageQueue = new BoundedQueue<ChatCompletionMessage>(remainingTokensForContextMessages);
        }

        return boundedMessageQueue;
    }

    /// <summary>
    /// Saves the bounded message queue for the channel to the cache.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <param name="boundedMessageQueue">The bounded message queue.</param>
    private void SaveBoundedMessageQueue(DiscordChannel channel, BoundedQueue<ChatCompletionMessage> boundedMessageQueue)
    {
        var cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);
        _cache.SetCache(cacheKey, boundedMessageQueue);
    }

    /// <summary>
    /// Checks if the message should be handled by Kattgpt.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>True if the message should be handled by Kattgpt.</returns>
    private bool ShouldHandleMessage(DiscordMessage message)
    {
        var channel = message.Channel;

        var channelOptions = _kattGptService.GetChannelOptions(channel);

        if (channelOptions == null)
        {
            return false;
        }

        // if the channel is not always on, handle the message
        if (!channelOptions.AlwaysOn)
        {
            return true;
        }

        // otherwise check if the message does not start with the MetaMessagePrefix
        var messageStartsWithMetaMessagePrefix = message.Content.TrimStart().StartsWith(MetaMessagePrefix);

        // if it does, return false
        return !messageStartsWithMetaMessagePrefix;
    }

    /// <summary>
    /// Checks if Kattgpt should reply to the message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>True if Kattgpt should reply.</returns>
    private bool ShouldReplyToMessage(DiscordMessage message)
    {
        var channel = message.Channel;

        var channelOptions = _kattGptService.GetChannelOptions(channel);

        if (channelOptions == null)
        {
            return false;
        }

        // if the channel is not always on
        if (!channelOptions.AlwaysOn)
        {
            // check if the current message is a reply to kattbot
            var messageIsReplyToKattbot = message.ReferencedMessage?.Author?.IsCurrent ?? false;

            if (messageIsReplyToKattbot)
            {
                return true;
            }

            // or if kattbot is mentioned
            var kattbotIsMentioned = message.MentionedUsers.Any(u => u.IsCurrent);

            return kattbotIsMentioned;
        }

        // otherwise check if the message does not start with the MetaMessagePrefix
        var messageStartsWithMetaMessagePrefix = message.Content.TrimStart().StartsWith(MetaMessagePrefix);

        // if it does, return false
        return !messageStartsWithMetaMessagePrefix;
    }
}
