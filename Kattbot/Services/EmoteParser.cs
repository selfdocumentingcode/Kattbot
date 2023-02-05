using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kattbot.Helpers;

namespace Kattbot.Services;

public class EmoteParser
{
    public EmoteParser()
    {
    }

    public List<string> ExtractEmotesFromMessage(string messageText)
    {
        MatchCollection result = EmoteHelper.EmoteRegex.Matches(messageText);

        var emojiStrings = result.Select(m => m.Value).ToList();

        return emojiStrings;
    }
}
