using DSharpPlus.Entities;
using Kattbot.Helper;
using Kattbot.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kattbot
{
    public class EmoteParser
    {
        public EmoteParser()
        {

        }

        public List<string> ExtractEmotesFromMessage(string messageText)
        {
            var result = EmoteHelper.EmoteRegex.Matches(messageText);

            var emojiStrings = result.Select(m => m.Value).ToList();

            return emojiStrings;
        }
    }
}
