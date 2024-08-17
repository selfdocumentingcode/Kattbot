using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kattbot.Common.Utils;

public static class StringExtensions
{
    public static string RemoveQuotes(this string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : input.Trim('"');
    }

    public static string? EscapeTicks(this string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? value : value.Replace(oldChar: '`', newChar: '\'');
    }

    public static List<string> SplitString(this string input, int chunkLength, string? splitToken = null)
    {
        if (splitToken != null && splitToken.Length > chunkLength)
        {
            throw new ArgumentException("Split token cannot be longer than chunk length");
        }

        var result = new List<string>();
        var sb = new StringBuilder();
        var sr = new StringReader(input);
        string st = !string.IsNullOrWhiteSpace(splitToken) ? splitToken : string.Empty;

        // Read the next word + the following whitespace character
        Func<string> readNextWord = () =>
        {
            var wordSb = new StringBuilder();
            while (true)
            {
                int c = sr.Read();

                if (c == -1)
                {
                    return wordSb.Length > 0 ? wordSb.ToString() : string.Empty;
                }

                wordSb.Append((char)c);

                if (char.IsWhiteSpace((char)c))
                {
                    return wordSb.ToString();
                }
            }
        };

        string word;

        while ((word = readNextWord()) != string.Empty)
        {
            if (sb.Length + word.Length > chunkLength)
            {
                result.Add(sb.ToString().TrimEnd());
                sb.Clear();
                sb.Append(st);
            }

            sb.Append(word);
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString().TrimEnd());
        }

        return result;
    }

    public static string ToSafeFilename(this string input, string extension)
    {
        string safeFilename = new(
            Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(input))
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray());

        var filename = $"{safeFilename}.{extension}";

        return filename;
    }

    public static StringBuilder AppendLines(this StringBuilder sb, IEnumerable<string> lines)
    {
        foreach (string line in lines)
        {
            sb.AppendLine(line);
        }

        return sb;
    }
}
