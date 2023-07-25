using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services.KattGpt;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TiktokenSharp;

namespace Kattbot.NotificationHandlers;

public class KattGptMessageHandler : INotificationHandler<MessageCreatedNotification>
{
    private const string ChatGptModel = "gpt-3.5-turbo-16k";
    private const string TokenizerModel = "gpt-3.5";
    private const string MetaMessagePrefix = "msg";
    private const float Temperature = 1.2f;
    private const int MaxTokens = 8192;
    private const int MaxTokensToGenerate = 960; // Roughly the limit of 2 Discord messages
    private const string ChannelWithTopicTemplateName = "ChannelWithTopic";
    private const string MessageSplitToken = "[cont.]";

    private readonly ChatGptHttpClient _chatGpt;
    private readonly KattGptOptions _kattGptOptions;
    private readonly KattGptChannelCache _cache;
    private readonly TikToken _tokenizer;

    public KattGptMessageHandler(
        ChatGptHttpClient chatGpt,
        IOptions<KattGptOptions> kattGptOptions,
        KattGptChannelCache cache)
    {
        _chatGpt = chatGpt;
        _kattGptOptions = kattGptOptions.Value;
        _cache = cache;

        _tokenizer = TikToken.EncodingForModel(TokenizerModel);
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

        var systemPromptsMessages = BuildSystemPromptsMessages(channel);

        var boundedMessageQueue = GetBoundedMessageQueue(channel, systemPromptsMessages);

        // Add new message from notification
        var newMessageContent = message.Content;
        var newMessageUser = author.GetNicknameOrUsername();

        var newUserMessage = ChatCompletionMessage.AsUser($"{newMessageUser}: {newMessageContent}");

        boundedMessageQueue.Enqueue(newUserMessage, _tokenizer.Encode(newUserMessage.Content).Count);

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
            boundedMessageQueue.Enqueue(chatGptResponse, _tokenizer.Encode(chatGptResponse.Content).Count);
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
    /// Builds the system prompts messages for the given channel.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The system prompts messages.</returns>
    private List<ChatCompletionMessage> BuildSystemPromptsMessages(DiscordChannel channel)
    {
        // Get core system prompt messages
        var coreSystemPrompts = string.Join(" ", _kattGptOptions.CoreSystemPrompts);
        var systemPromptsMessages = new List<ChatCompletionMessage>() { ChatCompletionMessage.AsSystem(coreSystemPrompts) };

        var guild = channel.Guild;
        var guildId = guild.Id;

        // Get the channel options for this guild
        var guildOptions = _kattGptOptions.GuildOptions.Where(x => x.Id == guildId).SingleOrDefault()
                            ?? throw new Exception($"No guild options found for guild {guildId}");

        // Get the guild system prompts if they exist
        string[] guildPromptsArray = guildOptions.SystemPrompts ?? Array.Empty<string>();

        // add them to the system prompts messages if not empty
        if (guildPromptsArray.Length > 0)
        {
            string guildSystemPrompts = string.Join(" ", guildPromptsArray);
            systemPromptsMessages.Add(ChatCompletionMessage.AsSystem(guildSystemPrompts));
        }

        var channelOptions = GetChannelOptions(channel);

        // if there are no channel options, return the system prompts messages
        if (channelOptions == null)
        {
            return systemPromptsMessages;
        }

        // get the system prompts for this channel
        string[] channelPromptsArray = channelOptions.SystemPrompts ?? Array.Empty<string>();

        // add them to the system prompts messages if not empty
        if (channelPromptsArray.Length > 0)
        {
            string channelSystemPrompts = string.Join(" ", channelPromptsArray);
            systemPromptsMessages.Add(ChatCompletionMessage.AsSystem(channelSystemPrompts));
        }

        // else if the channel options has UseChannelTopic set to true, add the channel topic to the system prompts messages
        else if (channelOptions.UseChannelTopic)
        {
            // get the channel topic or use a fallback
            var channelTopic = !string.IsNullOrWhiteSpace(channel.Topic) ? channel.Topic : "Whatever";

            // get the text template from kattgpt options
            var channelWithTopicTemplate = _kattGptOptions.Templates.Where(x => x.Name == ChannelWithTopicTemplateName).SingleOrDefault();

            // if the temmplate is not null, format it with the channel name and topic and add it to the system prompts messages
            if (channelWithTopicTemplate != null)
            {
                // get a sanitized channel name that only includes letters, digits, - and _
                var channelName = Regex.Replace(channel.Name, @"[^a-zA-Z0-9-_]", string.Empty);

                var formatedTemplatePrompt = string.Format(channelWithTopicTemplate.Content, channelName, channelTopic);
                systemPromptsMessages.Add(ChatCompletionMessage.AsSystem(formatedTemplatePrompt));
            }

            // else use the channelTopic as the system message
            else
            {
                systemPromptsMessages.Add(ChatCompletionMessage.AsSystem(channelTopic));
            }
        }

        return systemPromptsMessages;
    }

    /// <summary>
    /// Gets the bounded message queue for the channel from the cache or creates a new one.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <param name="systemPromptsMessages">The system prompts messages.</param>
    /// <returns>The bounded message queue for the channel.</returns>
    private BoundedQueue<ChatCompletionMessage> GetBoundedMessageQueue(DiscordChannel channel, List<ChatCompletionMessage> systemPromptsMessages)
    {
        var cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);
        var boundedMessageQueue = _cache.GetCache(cacheKey);
        if (boundedMessageQueue == null)
        {
            var totalTokenCountForSystemMessages = systemPromptsMessages.Select(x => x.Content).Sum(m => _tokenizer.Encode(m).Count);

            var remainingTokensForContextMessages = MaxTokens - totalTokenCountForSystemMessages;

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

        var channelOptions = GetChannelOptions(channel);

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

        var channelOptions = GetChannelOptions(channel);

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

    private ChannelOptions? GetChannelOptions(DiscordChannel channel)
    {
        var guild = channel.Guild;
        var guildId = guild.Id;

        // First check if kattgpt is enabled for this guild
        var guildOptions = _kattGptOptions.GuildOptions.Where(x => x.Id == guildId).SingleOrDefault();
        if (guildOptions == null)
        {
            return null;
        }

        var guildChannelOptions = guildOptions.ChannelOptions;
        var guildCategoryOptions = guildOptions.CategoryOptions;

        // Get the channel options for this channel or for the category this channel is in
        var channelOptions = guildChannelOptions.Where(x => x.Id == channel.Id).SingleOrDefault();
        if (channelOptions == null)
        {
            var category = channel.Parent;
            if (category != null)
            {
                channelOptions = guildCategoryOptions.Where(x => x.Id == category.Id).SingleOrDefault();
            }
        }

        return channelOptions;
    }
}
