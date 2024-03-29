using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Config;
using Kattbot.Helpers;
using Microsoft.Extensions.Options;

namespace Kattbot.Services.KattGpt;

public class KattGptService
{
    private const string ChannelContextWithTopicTemplate = "ChannelContextWithTopic";
    private const string ChannelContextWithoutTopicTemplate = "ChannelContextWithoutTopic";
    private const string ChannelGuidelinesHeaderTemplate = "ChannelGuidelines";

    private const string TemplateGuildNameToken = "{guildName}";
    private const string TemplateChannelNameToken = "{channelName}";
    private const string TemplateChannelTopicToken = "{channelTopic}";

    private readonly KattGptOptions _kattGptOptions;

    public KattGptService(IOptions<KattGptOptions> kattGptOptions)
    {
        _kattGptOptions = kattGptOptions.Value;
    }

    /// <summary>
    ///     Builds the system prompts messages for the given channel.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The system prompts messages.</returns>
    public List<ChatCompletionMessage> BuildSystemPromptsMessages(DiscordChannel channel)
    {
        var systemPromptBuilder = new StringBuilder();

        List<string> coreSystemPromptStringsa = _kattGptOptions.CoreSystemPrompts.ToList();
        systemPromptBuilder.AppendLines(coreSystemPromptStringsa);

        DiscordGuild guild = channel.Guild;
        ulong guildId = guild.Id;

        // Get the channel options for this guild
        GuildOptions guildOptions = _kattGptOptions.GuildOptions.Where(x => x.Id == guildId).SingleOrDefault()
                                    ?? throw new Exception($"No guild options found for guild {guildId}");

        // Get the guild display name
        string guildDisplayName = guildOptions.Name ?? channel.Guild.Name;

        var replaceArgs = new Dictionary<string, string>
        {
            { TemplateGuildNameToken, guildDisplayName },
        };

        ChannelOptions? channelOptions = GetChannelOptions(channel);

        // if there are no channel options, return the system prompts messages
        if (channelOptions != null)
        {
            // get a sanitized channel name that only includes letters, digits, - and _
            string channelDisplayName = Regex.Replace(channel.Name, @"[^a-zA-Z0-9-_]", string.Empty);

            string? channelTopic = channelOptions.Topic is not null
                ? channelOptions.Topic
                : channelOptions.FallbackToChannelTopic && !string.IsNullOrWhiteSpace(channel.Topic)
                    ? channel.Topic
                    : null;

            string channelContextTemplateName = channelTopic is not null
                ? ChannelContextWithTopicTemplate
                : ChannelContextWithoutTopicTemplate;

            Template? channelContextTemplate = _kattGptOptions.Templates
                .Where(x => x.Name == channelContextTemplateName)
                .SingleOrDefault();

            if (channelContextTemplate is not null)
            {
                systemPromptBuilder.AppendLine();
                systemPromptBuilder.AppendLine(channelContextTemplate.Content);
            }

            Template? headerTemplate = _kattGptOptions.Templates.Where(x => x.Name == ChannelGuidelinesHeaderTemplate)
                .SingleOrDefault();

            // get the system prompts for this channel
            string[] channelPromptStrings = channelOptions.SystemPrompts ?? Array.Empty<string>();

            if (headerTemplate is not null && channelPromptStrings.Length > 0)
            {
                systemPromptBuilder.AppendLine();
                systemPromptBuilder.AppendLine(headerTemplate.Content);
                systemPromptBuilder.AppendLines(channelPromptStrings);
            }

            replaceArgs.Add(TemplateChannelNameToken, channelDisplayName);
            replaceArgs.Add(TemplateChannelTopicToken, channelTopic ?? string.Empty);
        }

        var systemPromptsString = systemPromptBuilder.ToString();

        // replace variables
        foreach ((string key, string value) in replaceArgs)
        {
            systemPromptsString = systemPromptsString.Replace(key, value);
        }

        var systemPromptsMessages = new List<ChatCompletionMessage>
            { ChatCompletionMessage.AsSystem(systemPromptsString) };

        return systemPromptsMessages;
    }

    public ChannelOptions? GetChannelOptions(DiscordChannel channel)
    {
        DiscordGuild guild = channel.Guild;
        ulong guildId = guild.Id;

        // First check if kattgpt is enabled for this guild
        GuildOptions? guildOptions = _kattGptOptions.GuildOptions.Where(x => x.Id == guildId).SingleOrDefault();
        if (guildOptions == null)
        {
            return null;
        }

        ChannelOptions[] guildChannelOptions = guildOptions.ChannelOptions;
        ChannelOptions[] guildCategoryOptions = guildOptions.CategoryOptions;

        // Get the channel options for this channel or for the category this channel is in
        ChannelOptions? channelOptions = guildChannelOptions.Where(x => x.Id == channel.Id).SingleOrDefault();
        if (channelOptions == null)
        {
            DiscordChannel? category = channel.Parent;
            if (category is not null)
            {
                channelOptions = guildCategoryOptions.Where(x => x.Id == category.Id).SingleOrDefault();
            }
        }

        return channelOptions;
    }
}
