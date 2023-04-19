using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AI.Dev.OpenAI.GPT;
using DSharpPlus.Entities;
using Kattbot.Helpers;
using Kattbot.Services;
using Kattbot.Services.KattGpt;
using MediatR;
using Microsoft.Extensions.Options;

namespace Kattbot.NotificationHandlers;

public class KattGptMessageHandler : INotificationHandler<MessageCreatedNotification>
{
    private const string ChatGptModel = "gpt-3.5-turbo";
    private const string MetaMessagePrefix = "msg";
    private const int MaxTokensPerRequest = 2048;

    private readonly GuildSettingsService _guildSettingsService;
    private readonly ChatGptHttpClient _chatGpt;
    private readonly KattGptOptions _kattGptOptions;
    private readonly KattGptChannelCache _cache;

    public KattGptMessageHandler(
        GuildSettingsService guildSettingsService,
        ChatGptHttpClient chatGpt,
        IOptions<KattGptOptions> kattGptOptions,
        KattGptChannelCache cache)
    {
        _guildSettingsService = guildSettingsService;
        _chatGpt = chatGpt;
        _kattGptOptions = kattGptOptions.Value;
        _cache = cache;
    }

    public async Task Handle(MessageCreatedNotification notification, CancellationToken cancellationToken)
    {
        var args = notification.EventArgs;
        var message = args.Message;
        var author = args.Author;
        var channel = args.Message.Channel;

        if (!await ShouldHandleMessage(message, author, channel))
        {
            return;
        }

        // Get system prompt messages
        var systemPropmts = _kattGptOptions.SystemPrompts;
        var systemPromptsMessages = systemPropmts.Select(promptMessage => new ChatCompletionMessage { Role = "system", Content = promptMessage });

        // Get or create bounded queue
        var cacheKey = KattGptChannelCache.KattGptChannelCacheKey(channel.Id);
        var boundedMessageQueue = _cache.GetCache(cacheKey);

        if (boundedMessageQueue == null)
        {
            var totalTokenCountForSystemMessages = systemPropmts.Sum(m => GPT3Tokenizer.Encode(m).Count);

            var remainingTokensForContextMessages = MaxTokensPerRequest - totalTokenCountForSystemMessages;

            boundedMessageQueue = new BoundedQueue<ChatCompletionMessage>(remainingTokensForContextMessages);
        }

        // Add new message from notification
        var newMessageContent = message.Content;
        var newMessageUser = author.GetNicknameOrUsername();

        var newUserMessage = new ChatCompletionMessage { Role = "user", Content = $"{newMessageUser}: {newMessageContent}" };

        boundedMessageQueue.Enqueue(newUserMessage, GPT3Tokenizer.Encode(newUserMessage.Content).Count);

        // Collect request messages
        var requestMessages = new List<ChatCompletionMessage>();
        requestMessages.AddRange(systemPromptsMessages);
        requestMessages.AddRange(boundedMessageQueue.GetAll());

        // Make request
        var request = new ChatCompletionCreateRequest()
        {
            Model = ChatGptModel,
            Messages = requestMessages.ToArray(),
        };

        var response = await _chatGpt.ChatCompletionCreate(request);

        var responseMessage = response.Choices[0].Message;

        // Reply to user
        await message.RespondAsync(responseMessage.Content);

        // Add the chat gpt response message to the bounded queue
        boundedMessageQueue.Enqueue(responseMessage, GPT3Tokenizer.Encode(responseMessage.Content).Count);

        // Cache the message queue
        _cache.SetCache(cacheKey, boundedMessageQueue);
    }

    private async Task<bool> ShouldHandleMessage(DiscordMessage message, DiscordUser author, DiscordChannel channel)
    {
        if (author.IsBot || author.IsSystem.GetValueOrDefault())
        {
            return false;
        }

        var kattGptChannelId = await _guildSettingsService.GetKattGptChannelId(channel.Guild.Id);
        var channelIsKattGptChannel = !(kattGptChannelId == null || kattGptChannelId != channel.Id);

        if (channelIsKattGptChannel)
        {
            var isMetaMessage = message.Content.StartsWith(MetaMessagePrefix, StringComparison.OrdinalIgnoreCase);

            return !isMetaMessage;
        }

        var kattGptishChannelId = await _guildSettingsService.GetKattGptishChannelId(channel.Guild.Id);
        var channelIsKattGptishChannel = !(kattGptishChannelId == null || kattGptishChannelId != channel.Id);

        if (channelIsKattGptishChannel)
        {
            var messageIsReplyToKattbot = message.ReferencedMessage?.Author?.IsCurrent ?? false;

            if (messageIsReplyToKattbot)
            {
                return true;
            }

            var kattbotIsMentioned = message.MentionedUsers.Any(u => u.IsCurrent);

            if (kattbotIsMentioned)
            {
                return true;
            }
        }

        return false;
    }
}
