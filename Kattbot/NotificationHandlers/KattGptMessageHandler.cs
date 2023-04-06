﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kattbot.Helpers;
using Kattbot.Services;
using Kattbot.Services.KattGpt;
using MediatR;
using Microsoft.Extensions.Options;

namespace Kattbot.NotificationHandlers;

public class KattGptMessageHandler : INotificationHandler<MessageCreatedNotification>
{
    private const int CacheDurationMinutes = 60;
    private const string ChatGptModel = "gpt-3.5-turbo";
    private const string MetaMessagePrefix = "msg";

    private readonly GuildSettingsService _guildSettingsService;
    private readonly ChatGptHttpClient _chatGpt;
    private readonly KattGptOptions _kattGptOptions;
    private readonly KattGptCache _cache;

    public KattGptMessageHandler(
        GuildSettingsService guildSettingsService,
        ChatGptHttpClient chatGpt,
        IOptions<KattGptOptions> kattGptOptions,
        KattGptCache cache)
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

        if (author.IsBot || author.IsSystem.GetValueOrDefault())
        {
            return;
        }

        var kattGptChannelId = await _guildSettingsService.GetKattGptChannelId(args.Message.Channel.Guild.Id);

        if (kattGptChannelId == null || kattGptChannelId != args.Message.Channel.Id)
        {
            return;
        }

        if (message.Content.StartsWith(MetaMessagePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var messages = new List<ChatCompletionMessage>();

        // Add system prompt messages
        var systemPropmts = _kattGptOptions.SystemPrompts;

        messages.AddRange(systemPropmts.Select(promptMessage => new ChatCompletionMessage { Role = "system", Content = promptMessage }));

        var cacheKey = KattGptCache.MessageCacheKey(channel.Id);

        var messageCache = _cache.GetCache<KattGptMessageCacheQueue>(cacheKey) ?? new KattGptMessageCacheQueue();

        // Add previous messages from cache
        messages.AddRange(messageCache.GetAll());

        // Add new message from notification
        var newMessageContent = message.Content;
        var newMessageUser = author.GetNicknameOrUsername();

        var newUserMessage = new ChatCompletionMessage { Role = "user", Content = $"{newMessageUser}: {newMessageContent}" };

        messages.Add(newUserMessage);

        // Make request
        var request = new ChatCompletionCreateRequest()
        {
            Model = ChatGptModel,
            Messages = messages.ToArray(),
        };

        var response = await _chatGpt.ChatCompletionCreate(request);

        var responseMessage = response.Choices[0].Message;

        // Send message to Discord channel
        await channel.SendMessageAsync(responseMessage.Content);

        // Cache user message and chat gpt response message
        messageCache.Enqueue(newUserMessage);
        messageCache.Enqueue(responseMessage);

        // Cache message cache
        _cache.SetCache(cacheKey, messageCache, TimeSpan.FromMinutes(CacheDurationMinutes));
    }
}