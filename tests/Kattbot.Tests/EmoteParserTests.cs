using System.Collections.Generic;
using Kattbot.Helpers;
using Kattbot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kattbot.Tests;

[TestClass]
public class EmoteParserTests
{
    [TestMethod]
    public void ParseEmotes_WithMessageContaingEmotes_ReturnEmote()
    {
        const string testMessage = "Some text <:emoji_1:123123123>";

        List<string> emotes = EmoteHelper.ExtractEmotesFromMessage(testMessage);

        Assert.HasCount(expected: 1, emotes);
        Assert.AreEqual("<:emoji_1:123123123>", emotes[0]);
    }

    [TestMethod]
    public void ParseEmotes_WithMessageContaingThreeEmotes_ReturnThreeEmotes()
    {
        const string testMessage = "Some text <:emoji_1:123123123> other <:emoji_2:123123123><:emoji_3:123123123>";

        List<string> emotes = EmoteHelper.ExtractEmotesFromMessage(testMessage);

        Assert.HasCount(expected: 3, emotes);
    }
}
