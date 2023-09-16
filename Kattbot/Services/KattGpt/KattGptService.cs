using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DSharpPlus.Entities;
using Microsoft.Extensions.Options;
using TiktokenSharp;

namespace Kattbot.Services.KattGpt;

public class KattGptService
{
    private const string ChannelWithTopicTemplateName = "ChannelWithTopic";
    private const string TokenizerModel = "gpt-3.5";

    private readonly KattGptOptions _kattGptOptions;

    public KattGptService(IOptions<KattGptOptions> kattGptOptions)
    {
        _kattGptOptions = kattGptOptions.Value;
    }

    /// <summary>
    /// Builds the system prompts messages for the given channel.
    /// </summary>
    /// <param name="channel">The channel.</param>
    /// <returns>The system prompts messages.</returns>
    public List<ChatCompletionMessage> BuildSystemPromptsMessages(DiscordChannel channel)
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

    public ChannelOptions? GetChannelOptions(DiscordChannel channel)
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

    public int GetTokenCount(string messageText)
    {
        var tokenizer = TikToken.EncodingForModel(TokenizerModel);

        return tokenizer.Encode(messageText).Count;
    }

    public int GetTokenCount(IEnumerable<ChatCompletionMessage> systemMessage)
    {
        var tokenizer = TikToken.EncodingForModel(TokenizerModel);

        var totalTokenCountForSystemMessages = systemMessage.Select(x => x.Content).Sum(m => tokenizer.Encode(m).Count);

        return totalTokenCountForSystemMessages;
    }
}
