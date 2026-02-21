using System;
using System.Collections.Generic;
using Kattbot.Common.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Kattbot.Tests;

[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    [DataRow("This is a test string", 10, new[] { "This is a", "test", "string" })]
    [DataRow("This is a test string", 12, new[] { "This is a", "test string" })]
    [DataRow("This is another test string", 10, new[] { "This is", "another", "test", "string" })]
    public void SplitString_WithValidStringInput_ReturnsExpectedStrings(
        string input,
        int chunkLength,
        string[] expected)
    {
        // Act
        List<string> actual = input.SplitString(chunkLength);

        // Assert
        CollectionAssert.AreEqual(expected.AsReadOnly(), actual);
    }

    [TestMethod]
    [DataRow("This is a test string", 10, new[] { "This is a", "[wat]test", "[wat]string" })]
    [DataRow("This is a test string", 12, new[] { "This is a", "[wat]test", "[wat]string" })]
    [DataRow("This is another test string", 10, new[] { "This is", "[wat]another", "[wat]test", "[wat]string" })]
    public void SplitString_WithValidInputAndSplitToken_ReturnsExpectedStrings(
        string input,
        int chunkLength,
        string[] expected)
    {
        // Arrange
        const string splitToken = "[wat]";

        // Act
        List<string> actual = input.SplitString(chunkLength, splitToken);

        // Assert
        CollectionAssert.AreEqual(expected.AsReadOnly(), actual);
    }

    [TestMethod]
    public void SplitString_WithEmptyStringInput_ReturnsEmptyList()
    {
        // Arrange
        var input = string.Empty;
        var chunkLength = 1;
        var expected = new List<string>();

        // Act
        List<string> actual = input.SplitString(chunkLength);

        // Assert
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void SplitString_WithSplitTokenLongerThanChunkLength_ThrowsArgumentException()
    {
        // Arrange
        var input = string.Empty;
        const int chunkLength = 1;
        const string token = "ab";

        // Act
        Func<List<string>> actual = () => input.SplitString(chunkLength, token);

        // Assert
        Assert.Throws<ArgumentException>(actual);
    }
}
