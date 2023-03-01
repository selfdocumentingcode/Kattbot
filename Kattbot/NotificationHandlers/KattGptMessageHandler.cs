using System;
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

        if (message.Content.Contains("[meta]", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var messages = new List<ChatCompletionMessage>();

        // Add system prompt messages
        var systemPropmts = _kattGptOptions.SystemPrompts;

        messages.AddRange(systemPropmts.Select(promptMessage => new ChatCompletionMessage { Role = "system", Content = promptMessage }));

        // Add previous messages from cache
        var prevMessages = (_cache.GetCache<ChatCompletionMessage[]>(KattGptCache.CacheKey) ?? Array.Empty<ChatCompletionMessage>())
                            .ToList();

        messages.AddRange(prevMessages);

        // Add new message from notification
        var newMessageContent = message.Content;
        var newMessageUser = author.GetNicknameOrUsername();

        var newUserMessage = new ChatCompletionMessage { Role = "user", Content = $"[{newMessageUser}]: {newMessageContent}" };

        messages.Add(newUserMessage);

        var request = new ChatCompletionCreateRequest()
        {
            Model = "gpt-3.5-turbo",
            Messages = messages.ToArray(),
        };

        var response = await _chatGpt.ChatCompletionCreate(request);

        var responseMessage = response.Choices[0].Message;

        await channel.SendMessageAsync(responseMessage.Content);

        // Cache user message and chat gpt response message
        prevMessages.Add(newUserMessage);
        prevMessages.Add(responseMessage);

        _cache.SetCache(KattGptCache.CacheKey, prevMessages.ToArray(), TimeSpan.FromMinutes(10));
    }
}
