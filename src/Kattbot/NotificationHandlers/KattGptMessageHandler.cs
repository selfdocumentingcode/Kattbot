using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Common.Utils;
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
    private const float DefaultTemperature = 1.1f;
    private const int MaxTotalTokens = 24_576;
    private const int MaxTokensToGenerate = 960; // Roughly the limit of 2 Discord messages
    private const string MessageSplitToken = "[cont.] ";
    private const string RecipientMarkerToYou = "[to you]";
    private const string RecipientMarkerToOthers = "[to others]";
    private const string MessageToolUseTemplate = "`Kattbot used: {0}`";

    private readonly KattGptChannelCache _cache;
    private readonly ChatGptHttpClient _chatGpt;
    private readonly DalleHttpClient _dalleHttpClient;
    private readonly DiscordErrorLogger _discordErrorLogger;
    private readonly ImageService _imageService;
    private readonly KattGptOptions _kattGptOptions;
    private readonly KattGptService _kattGptService;
    private readonly BotOptions _botOptions;

    public KattGptMessageHandler(
        ChatGptHttpClient chatGpt,
        KattGptChannelCache cache,
        KattGptService kattGptService,
        DalleHttpClient dalleHttpClient,
        ImageService imageService,
        DiscordErrorLogger discordErrorLogger,
        IOptions<KattGptOptions> kattGptOptions,
        IOptions<BotOptions> botOptions)
    {
        _chatGpt = chatGpt;
        _cache = cache;
        _kattGptService = kattGptService;
        _dalleHttpClient = dalleHttpClient;
        _imageService = imageService;
        _discordErrorLogger = discordErrorLogger;
        _kattGptOptions = kattGptOptions.Value;
        _botOptions = botOptions.Value;
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        MessageCreatedEventArgs? args = notification.EventArgs;
        DiscordMessage? message = args.Message;
        DiscordUser? author = args.Author;
        DiscordChannel? channel = args.Message.Channel ?? throw new Exception("Channel is null.");

        if (!ShouldHandleMessage(message)) return;

        try
        {
            List<ChatCompletionMessage>? systemPromptsMessages = _kattGptService.BuildSystemPromptsMessages(channel);
            ChatCompletionFunction? chatCompletionFunction = DalleToolBuilder.BuildDalleImageToolDefinition().Function;
            List<ChatCompletionMessage> newContextMessages = [];

            KattGptChannelContext? channelContext = GetOrCreateCachedContext(
                channel,
                systemPromptsMessages,
                chatCompletionFunction);

            bool shouldReplyToMessage = ShouldReplyToMessage(message);

            string? recipientMarker = shouldReplyToMessage
                ? RecipientMarkerToYou
                : RecipientMarkerToOthers;

            // Add new message from notification
            string? newMessageUser = author.GetDisplayName();
            string? newMessageContent = message.SubstituteMentions();

            ChatCompletionMessage? newUserMessage =
                ChatCompletionMessage.AsUser($"{newMessageUser}{recipientMarker}: {newMessageContent}");

            newContextMessages.Add(newUserMessage);

            if (!shouldReplyToMessage)
            {
                channelContext.AddMessages(newContextMessages);
                return;
            }

            await channel.TriggerTypingAsync();

            ChatCompletionCreateRequest? request = BuildRequest(
                systemPromptsMessages,
                channelContext,
                allowToolCalls: true,
                newUserMessage);

            ChatCompletionCreateResponse? response = await _chatGpt.ChatCompletionCreate(request);

            ChatCompletionChoice? chatGptResponse = response.Choices[0];
            ChatCompletionMessage? chatGptResponseMessage = chatGptResponse.Message;

            newContextMessages.Add(chatGptResponseMessage);

            if (chatGptResponse.FinishReason == ChoiceFinishReason.tool_calls)
            {
                List<ChatCompletionMessage>? toolResponseMessages = await HandleToolCallResponse(
                    message,
                    systemPromptsMessages,
                    channelContext,
                    chatGptResponseMessage);

                newContextMessages.AddRange(toolResponseMessages);
            }
            else
            {
                // TODO: Handle other finish reasons
                await SendTextReply(chatGptResponseMessage.Content!, message);
            }

            // If everything went well, add the new messages to the context
            channelContext.AddMessages(newContextMessages);
        }
        catch (Exception ex)
        {
            await SendTextReply($"Something went wrong: {ex.Message}", message);
            _discordErrorLogger.LogError(ex.Message);
        }
    }

    private static ChatCompletionCreateRequest BuildRequest(
        List<ChatCompletionMessage> systemPromptsMessages,
        KattGptChannelContext channelContext,
        bool allowToolCalls = true,
        params ChatCompletionMessage[] newMessages)
    {
        // The tools field in the request is not allowed to be an empty array
        ChatCompletionTool[]? chatCompletionTools = allowToolCalls
            ? [DalleToolBuilder.BuildDalleImageToolDefinition()]
            : null;

        // Not allowed to include parallel tool calls field when tools is null
        bool? parallelToolCalls = allowToolCalls ? false : null;

        // Collect request messages
        var requestMessages = new List<ChatCompletionMessage>();
        requestMessages.AddRange(systemPromptsMessages);
        requestMessages.AddRange(channelContext.GetMessages());
        requestMessages.AddRange(newMessages);

        // Make request
        var request = new ChatCompletionCreateRequest
        {
            Model = ChatGptModel,
            Messages = requestMessages.ToArray(),
            Temperature = DefaultTemperature,
            MaxTokens = MaxTokensToGenerate,
            Tools = chatCompletionTools,
            ParallelToolCalls = parallelToolCalls,
        };

        return request;
    }

    private static async Task SendImageReply(
        string responseMessageText,
        DiscordMessage messageToReplyTo,
        string filename,
        ImageStreamResult imageStream)
    {
        const int maxFilenameLength = 32;

        string? truncatedFilename = filename.Length > maxFilenameLength
            ? filename[..maxFilenameLength]
            : filename;

        string? safeFilename = truncatedFilename.ToSafeFilename(imageStream.FileExtension);

        DiscordMessageBuilder? mb = new DiscordMessageBuilder()
            .AddFile(safeFilename, imageStream.MemoryStream)
            .WithContent(responseMessageText);

        await messageToReplyTo.RespondAsync(mb);
    }

    private static async Task SendTextReply(string responseMessage, DiscordMessage messageToReplyTo)
    {
        List<string>? messageChunks = responseMessage.SplitString(DiscordConstants.MaxMessageLength, MessageSplitToken);

        DiscordMessage? nextMessageToReplyTo = messageToReplyTo;

        foreach (string? messageChunk in messageChunks)
        {
            nextMessageToReplyTo = await nextMessageToReplyTo.RespondAsync(messageChunk);
        }
    }

    private static async Task SendToolUseReply(
        DiscordMessage message,
        ChatCompletionMessage chatGptToolCallResponse,
        string prompt)
    {
        string? toolUseText = string.Format(MessageToolUseTemplate, prompt);
        string? responseMessageText = chatGptToolCallResponse.Content ?? string.Empty;

        // Tool call messages have content only sometimes
        string? responseTextWithToolUse = !string.IsNullOrWhiteSpace(responseMessageText)
            ? $"{responseMessageText.TrimEnd()}\n\n{toolUseText}"
            : toolUseText;

        await message.RespondAsync(responseTextWithToolUse);
    }

    private async Task<ImageStreamResult> GetDalleResult(string prompt, string userId)
    {
        var imageRequest = new CreateImageRequest
        {
            Prompt = prompt,
            Model = CreateImageModel,
            User = userId,
        };

        CreateImageResponse? response = await _dalleHttpClient.CreateImage(imageRequest);
        if (response.Data == null || !response.Data.Any()) throw new Exception("Empty result");

        ImageResponseUrlData? imageUrl = response.Data.First();

        Image? image = await _imageService.DownloadImage(imageUrl.Url);

        ImageStreamResult? imageStream = await _imageService.GetImageStream(image);

        return imageStream;
    }

    private async Task<List<ChatCompletionMessage>> HandleToolCallResponse(
        DiscordMessage message,
        List<ChatCompletionMessage> systemPromptsMessages,
        KattGptChannelContext channelContext,
        ChatCompletionMessage chatGptToolCallResponse)
    {
        List<ChatCompletionMessage> responseMessages = [];

        ChatCompletionToolCall? toolCall =
            chatGptToolCallResponse.ToolCalls?[0] ?? throw new Exception("Tool call is null.");

        if (chatGptToolCallResponse.ToolCalls.Count > 1)
            throw new Exception($"Too many tool calls: {chatGptToolCallResponse.ToolCalls.Count.ToString()}");

        // Parse the function call arguments
        string? functionCallArguments = toolCall.FunctionCall.Arguments;

        JsonNode? parsedArguments = JsonNode.Parse(functionCallArguments)
                                    ?? throw new Exception("Could not parse function call arguments.");

        string? prompt = parsedArguments["prompt"]?.GetValue<string>()
                         ?? throw new Exception("Function call arguments are invalid.");

        // Send the tool use message as a confirmation
        await SendToolUseReply(message, chatGptToolCallResponse, prompt);

        var authorId = message.Author!.Id.ToString();

        ImageStreamResult? dalleResult = await GetDalleResult(prompt, authorId);

        // Build the function call result message
        var functionCallResult = $"An image of {prompt} has been generated and attached to this message.";

        ChatCompletionMessage? functionCallResultMessage =
            ChatCompletionMessage.AsToolCallResult(functionCallResult, toolCall.Id);

        // Force a content value for the ChatGPT response due the api not allowing nulls even though it says it does
        chatGptToolCallResponse.Content ??= "null";

        ChatCompletionCreateRequest? request = BuildRequest(
            systemPromptsMessages,
            channelContext,
            allowToolCalls: false,
            chatGptToolCallResponse,
            functionCallResultMessage);

        ChatCompletionCreateResponse? response = await _chatGpt.ChatCompletionCreate(request);

        // Handle new response
        ChatCompletionMessage? functionCallResponse = response.Choices[0].Message;

        await SendImageReply(functionCallResponse.Content!, message, prompt, dalleResult);

        // Return the function messages
        responseMessages.Add(functionCallResultMessage);
        responseMessages.Add(functionCallResponse);

        return responseMessages;
    }

    /// <summary>
    ///     Gets the channel context from the cache or creates a new one.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <param name="systemPromptsMessages">The list of system prompts messages.</param>
    /// <param name="chatCompletionFunction">The tool function gpt can call.</param>
    /// <returns>The bounded message queue for the channel.</returns>
    private KattGptChannelContext GetOrCreateCachedContext(
        DiscordChannel channel,
        List<ChatCompletionMessage> systemPromptsMessages,
        ChatCompletionFunction chatCompletionFunction)
    {
        string? cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);

        KattGptChannelContext? channelContext = _cache.GetCache(cacheKey);

        if (channelContext != null)
            return channelContext;

        var kattGptTokenizer = new KattGptTokenizer(TokenizerModel);

        int systemMessagesTokenCount = kattGptTokenizer.GetTokenCount(systemPromptsMessages);

        int functionsTokenCount =
            kattGptTokenizer.GetTokenCount(chatCompletionFunction);

        int reservedTokenCount = systemMessagesTokenCount + functionsTokenCount;

        int remainingTokensForContextMessages = MaxTotalTokens - MaxTokensToGenerate - reservedTokenCount;

        var tokenizer = new KattGptTokenizer(ChatGptModel);

        channelContext = new KattGptChannelContext(remainingTokensForContextMessages, tokenizer);

        _cache.SetCache(cacheKey, channelContext);

        return channelContext;
    }

    /// <summary>
    ///     Checks if the message should be handled by KattGpt.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>True if the message should be handled by KattGpt.</returns>
    private bool ShouldHandleMessage(DiscordMessage message)
    {
        if (!IsRelevantMessage(message)) return false;

        string[] commandPrefixes = [_botOptions.CommandPrefix, _botOptions.AlternateCommandPrefix];
        string? messageContent = message.Content.ToLower().TrimStart();

        bool messageStartsWithCommandPrefix = commandPrefixes.Any(messageContent.StartsWith);

        if (messageStartsWithCommandPrefix)
            return false;

        DiscordChannel? channel = message.Channel!;

        ChannelOptions? channelOptions = _kattGptService.GetChannelOptions(channel);

        if (channelOptions == null) return false;

        // if the channel is not always on, handle the message
        if (!channelOptions.AlwaysOn) return true;

        // otherwise check if the message does not start with the MetaMessagePrefix
        string[]? metaMessagePrefixes = _kattGptOptions.AlwaysOnIgnoreMessagePrefixes;
        bool messageStartsWithMetaMessagePrefix = metaMessagePrefixes.Any(messageContent.StartsWith);

        // if it does, return false
        return !messageStartsWithMetaMessagePrefix;
    }

    /// <summary>
    ///     Checks if KattGpt should reply to the message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>True if KattGpt should reply.</returns>
    private bool ShouldReplyToMessage(DiscordMessage message)
    {
        DiscordChannel? channel = message.Channel!;

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
        string[]? metaMessagePrefixes = _kattGptOptions.AlwaysOnIgnoreMessagePrefixes;
        bool messageStartsWithMetaMessagePrefix = metaMessagePrefixes.Any(message.Content.TrimStart().StartsWith);

        // if it does, return false
        return !messageStartsWithMetaMessagePrefix;
    }
}
