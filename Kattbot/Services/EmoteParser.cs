using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kattbot.Helpers;

namespace Kattbot.Services;

public class EmoteParser
{
    public List<string> ExtractEmotesFromMessage(string messageText)
    {
        MatchCollection result = EmoteHelper.EmoteRegex.Matches(messageText);

        List<string> emojiStrings = result.Select(m => m.Value).ToList();

        return emojiStrings;
    }
}
