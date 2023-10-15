using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Kattbot.Helpers;

public static class StringExtensions
{
    public static string RemoveQuotes(this string input)
    {
        return string.IsNullOrEmpty(input) ? string.Empty : input.Trim('"');
    }

    public static List<string> SplitString(this string input, int chunkLength, string? splitToken = null)
    {
        if (splitToken != null && splitToken.Length > chunkLength)
            throw new ArgumentException("Split token cannot be longer than chunk length");

        var result = new List<string>();
        var sb = new StringBuilder();
        var sr = new StringReader(input);
        var st = !string.IsNullOrWhiteSpace(splitToken) ? splitToken : string.Empty;

        // Read the next word + the following whitespace character
        var readNextWord = () =>
        {
            var sb = new StringBuilder();
            while (true)
            {
                int c = sr.Read();

                if (c == -1) // End of string
                    return sb.Length > 0 ? sb.ToString() : string.Empty;

                sb.Append((char)c);

                if (char.IsWhiteSpace((char)c))
                    return sb.ToString();
            }
        };

        string word;

        while ((word = readNextWord()) != string.Empty)
        {
            if ((sb.Length + word.Length) > chunkLength)
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
        string safeFilename = new(Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(input))
                                    .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                                    .ToArray());

        string filename = $"{safeFilename}.{extension}";

        return filename;
    }

    public static StringBuilder AppendLines(this StringBuilder sb, IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }

        return sb;
    }
}
