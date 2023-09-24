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

        var tokenizer = TikToken.EncodingForModel(_modelName);

        return tokenizer.Encode(messageText).Count;
    }

    public int GetTokenCount(string component1, string component2)
    {
        var tokenizer = TikToken.EncodingForModel(_modelName);

        var count1 = tokenizer.Encode(component1).Count;
        var count2 = tokenizer.Encode(component2).Count;

        return count1 + count2;
    }

    public int GetTokenCount(string component1, string component2, string component3)
    {
        var tokenizer = TikToken.EncodingForModel(_modelName);

        var count1 = tokenizer.Encode(component1).Count;
        var count2 = tokenizer.Encode(component2).Count;
        var count3 = tokenizer.Encode(component3).Count;

        return count1 + count2 + count3;
    }

    public int GetTokenCount(FunctionCall functionCall)
    {
        var tokenizer = TikToken.EncodingForModel(_modelName);

        var nameTokenCount = tokenizer.Encode(functionCall.Name).Count;
        var argumentsTokenCount = tokenizer.Encode(functionCall.Arguments).Count;

        return nameTokenCount + argumentsTokenCount;
    }

    public int GetTokenCount(IEnumerable<ChatCompletionMessage> systemMessage)
    {
        var tokenizer = TikToken.EncodingForModel(_modelName);

        var totalTokenCountForSystemMessages = systemMessage.Select(x => x.Content).Sum(m => tokenizer.Encode(m).Count);

        return totalTokenCountForSystemMessages;
    }
}
