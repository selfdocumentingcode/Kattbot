using System.Collections.Generic;
using System.Linq;
using Kattbot.Common.Models.KattGpt;
using Kattbot.Helpers;

namespace Kattbot.Services.KattGpt;

public class KattGptChannelContext
{
    private readonly KattGptTokenizer _tokenizer;
    private readonly BoundedQueue<ChatCompletionMessage> _chatMessages;

    public KattGptChannelContext(int contextSizeTokens, KattGptTokenizer tokenizer)
    {
        _tokenizer = tokenizer;
        _chatMessages = new BoundedQueue<ChatCompletionMessage>(contextSizeTokens);
    }

    public void AddMessage(ChatCompletionMessage item)
    {
        int itemSize = _tokenizer.GetTokenCount(item.Content);
        _chatMessages.Enqueue(item, itemSize);
    }

    public void AddMessages(IEnumerable<ChatCompletionMessage> items)
    {
        foreach (ChatCompletionMessage item in items)
        {
            int itemSize = _tokenizer.GetTokenCount(item.Content);
            _chatMessages.Enqueue(item, itemSize);
        }
    }

    public List<ChatCompletionMessage> GetMessages()
    {
        return _chatMessages.GetAll().ToList();
    }
}
