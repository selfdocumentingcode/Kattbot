using System.Collections.Generic;
using System.Linq;
using Kattbot.Common.Models.KattGpt;
using TiktokenSharp;

namespace Kattbot.Services.KattGpt;

public class KattGptTokenizer
{
    private readonly string _modelName;

    public KattGptTokenizer(string modelName)
    {
        _modelName = modelName;
    }

    public int GetTokenCount(string? messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
        {
            return 0;
        }

        TikToken tokenizer = TikToken.EncodingForModel(_modelName);

        return tokenizer.Encode(messageText).Count;
    }

    public int GetTokenCount(string component1, string component2)
    {
        TikToken tokenizer = TikToken.EncodingForModel(_modelName);

        int count1 = tokenizer.Encode(component1).Count;
        int count2 = tokenizer.Encode(component2).Count;

        return count1 + count2;
    }

    public int GetTokenCount(string component1, string component2, string component3)
    {
        TikToken tokenizer = TikToken.EncodingForModel(_modelName);

        int count1 = tokenizer.Encode(component1).Count;
        int count2 = tokenizer.Encode(component2).Count;
        int count3 = tokenizer.Encode(component3).Count;

        return count1 + count2 + count3;
    }

    public int GetTokenCount(ChatCompletionFunction functionCall)
    {
        TikToken tokenizer = TikToken.EncodingForModel(_modelName);

        int nameTokenCount = tokenizer.Encode(functionCall.Name).Count;
        int argumentsTokenCount = tokenizer.Encode(functionCall.Parameters.ToString()).Count;

        return nameTokenCount + argumentsTokenCount;
    }

    public int GetTokenCount(IEnumerable<ChatCompletionMessage> systemMessage)
    {
        TikToken tokenizer = TikToken.EncodingForModel(_modelName);

        int totalTokenCountForSystemMessages = systemMessage.Select(x => x.Content).Sum(m => tokenizer.Encode(m).Count);

        return totalTokenCountForSystemMessages;
    }
}
