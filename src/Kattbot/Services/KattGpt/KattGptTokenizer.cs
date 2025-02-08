using System.Collections.Generic;
using System.Linq;
using Kattbot.Common.Models.KattGpt;
using TiktokenSharp;

namespace Kattbot.Services.KattGpt;

public class KattGptTokenizer
{
    private readonly TikToken _tokenizer;

    public KattGptTokenizer(string modelName)
    {
        _tokenizer = TikToken.EncodingForModel(modelName);
    }

    public int GetTokenCount(string? messageText)
    {
        return string.IsNullOrWhiteSpace(messageText) ? 0 : _tokenizer.Encode(messageText).Count;
    }

    public int GetTokenCount(string component1, string component2, string component3)
    {
        int count1 = _tokenizer.Encode(component1).Count;
        int count2 = _tokenizer.Encode(component2).Count;
        int count3 = _tokenizer.Encode(component3).Count;

        return count1 + count2 + count3;
    }

    public int GetTokenCount(ChatCompletionFunction functionCall)
    {
        int nameTokenCount = _tokenizer.Encode(functionCall.Name).Count;
        int descriptionTokenCount = _tokenizer.Encode(functionCall.Description ?? string.Empty).Count;
        int argumentsTokenCount = _tokenizer.Encode(functionCall.Parameters.ToString()).Count;

        return nameTokenCount + +descriptionTokenCount + argumentsTokenCount;
    }

    public int GetTokenCount(IEnumerable<ChatCompletionMessage> systemMessage)
    {
        int totalTokenCountForSystemMessages =
            systemMessage.Select(x => x.Content).Sum(m => _tokenizer.Encode(m).Count);

        return totalTokenCountForSystemMessages;
    }
}
