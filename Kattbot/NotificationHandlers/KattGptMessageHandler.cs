using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Helpers;
using Kattbot.Services.KattGpt;
using MediatR;
using Microsoft.Extensions.Options;

namespace Kattbot.NotificationHandlers;

public class KattGptMessageHandler : INotificationHandler<MessageCreatedNotification>
{
    private const string ChatGptModel = "gpt-3.5-turbo-16k";
    private const string MetaMessagePrefix = "msg";
    private const float Temperature = 1.2f;
    private const int MaxTokens = 8192;
    private const int MaxTokensToGenerate = 960; // Roughly the limit of 2 Discord messages
    private const string MessageSplitToken = "[cont.]";

    private readonly ChatGptHttpClient _chatGpt;
    private readonly KattGptOptions _kattGptOptions;
    private readonly KattGptChannelCache _cache;
    private readonly KattGptService _kattGptService;

    public KattGptMessageHandler(
        ChatGptHttpClient chatGpt,
        IOptions<KattGptOptions> kattGptOptions,
        KattGptChannelCache cache,
        KattGptService kattGptService)
    {
        _chatGpt = chatGpt;
        _kattGptOptions = kattGptOptions.Value;
        _cache = cache;
        _kattGptService = kattGptService;
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

        var systemPromptsMessages = _kattGptService.BuildSystemPromptsMessages(channel);

        var systemMessagesTokenCount = _kattGptService.GetTokenCount(systemPromptsMessages);

        var boundedMessageQueue = GetBoundedMessageQueue(channel, systemMessagesTokenCount);

        // Add new message from notification
        var newMessageUser = author.GetDisplayName();
        var newMessageContent = message.GetMessageWithTextMentions();

        var newUserMessage = ChatCompletionMessage.AsUser($"{newMessageUser}: {newMessageContent}");

        boundedMessageQueue.Enqueue(newUserMessage, _kattGptService.GetTokenCount(newUserMessage.Content));

        if (ShouldReplyToMessage(message))
        {
            await channel.TriggerTypingAsync();

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
            };

            var response = await _chatGpt.ChatCompletionCreate(request);

            var chatGptResponse = response.Choices[0].Message;

            await SendReply(chatGptResponse.Content, message);

            // Add the chat gpt response message to the bounded queue
            boundedMessageQueue.Enqueue(chatGptResponse, _kattGptService.GetTokenCount(chatGptResponse.Content));
        }

        SaveBoundedMessageQueue(channel, boundedMessageQueue);
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
        var messageStartsWithMetaMessagePrefix = message.Content.StartsWith(MetaMessagePrefix);

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
        var messageStartsWithMetaMessagePrefix = message.Content.StartsWith(MetaMessagePrefix);

        // if it does, return false
        return !messageStartsWithMetaMessagePrefix;
    }
}
