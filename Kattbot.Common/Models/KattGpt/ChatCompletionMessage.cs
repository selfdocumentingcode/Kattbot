﻿using System.Text.Json.Serialization;

namespace Kattbot.Common.Models.KattGpt;

public record ChatCompletionMessage
{
    public ChatCompletionMessage(string role, string? content)
    {
        Role = role;
        Content = content;
    }

    public ChatCompletionMessage(string role, string name, string content)
    {
        Role = role;
        Name = name;
        Content = content;
    }

    [JsonConstructor]
    public ChatCompletionMessage(string role, string name, string? content, FunctionCall? functionCall)
        : this(role, content)
    {
        FunctionCall = functionCall;
        Name = name;
    }

    /// <summary>
    ///     Gets the role of the messages author. One of system, user, assistant, or function.
    ///     https://platform.openai.com/docs/api-reference/chat/create#messages-role.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; } = null!;

    /// <summary>
    ///     Gets or sets the contents of the message. content is required for all messages, and may be null for assistant
    ///     messages with function calls.
    ///     https://platform.openai.com/docs/api-reference/chat/create#messages-content.
    /// </summary>
    [JsonPropertyName("content")]
    public string?
        Content
    {
        get;
        set;
    } // This should be private but the api is being weird and doesn't allow nulls like it says it does

    /// <summary>
    ///     Gets the name of the author of this message. name is required if role is function, and it should be the name of the
    ///     function whose response is in the content.
    ///     May contain a-z, A-Z, 0-9, and underscores, with a maximum length of 64 characters.
    ///     https://platform.openai.com/docs/api-reference/chat/create#messages-name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; }

    /// <summary>
    ///     Gets the name and arguments of a function that should be called, as generated by the model.
    ///     https://platform.openai.com/docs/api-reference/chat/create#messages-function_call.
    /// </summary>
    [JsonPropertyName("function_call")]
    public FunctionCall? FunctionCall { get; }

    public static ChatCompletionMessage AsSystem(string content)
    {
        return new ChatCompletionMessage("system", content);
    }

    public static ChatCompletionMessage AsUser(string content)
    {
        return new ChatCompletionMessage("user", content);
    }

    public static ChatCompletionMessage AsAssistant(string content)
    {
        return new ChatCompletionMessage("assistant", content);
    }

    /// <summary>
    ///     Builds a message as a function call which contains the function result to be added to the context.
    /// </summary>
    /// <param name="name">The name of the function.</param>
    /// <param name="content">The result of the function.</param>
    /// <returns>A <see cref="ChatCompletionMessage" />.</returns>
    public static ChatCompletionMessage AsFunctionCallResult(string name, string content)
    {
        return new ChatCompletionMessage("function", name, content);
    }
}
