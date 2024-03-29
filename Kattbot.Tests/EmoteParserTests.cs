using System.Collections.Generic;
using Kattbot.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kattbot.Tests;

[TestClass]
public class EmoteParserTests
{
    private EmoteParser _sut = null!;

    [TestInitialize]
    public void Initialize()
    {
        _sut = new EmoteParser();
    }

    [TestMethod]
    public void ParseEmotes_WithMessageContaingEmotes_ReturnEmote()
    {
        var testMessage = "Some text <:emoji_1:123123123>";

        List<string> emotes = _sut.ExtractEmotesFromMessage(testMessage);

        Assert.AreEqual(emotes.Count, 1);
        Assert.AreEqual(emotes[0], "<:emoji_1:123123123>");
    }

    [TestMethod]
    public void ParseEmotes_WithMessageContaingThreeEmotes_ReturnThreeEmotes()
    {
        var testMessage = "Some text <:emoji_1:123123123> other <:emoji_2:123123123><:emoji_3:123123123>";

        List<string> emotes = _sut.ExtractEmotesFromMessage(testMessage);

        Assert.AreEqual(emotes.Count, 3);
    }
}
