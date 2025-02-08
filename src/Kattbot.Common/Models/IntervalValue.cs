using System;
using System.Text.RegularExpressions;

namespace Kattbot.Common.Models;

public class IntervalValue
{
    public static readonly string IntervalLifetimeToken = "lifetime";
    private static readonly Regex IntervalRegex = new(@"^(\d+)(m|w|d)$", RegexOptions.IgnoreCase);

    private IntervalValue(int value, string unit)
    {
        Unit = unit.ToLower();
        NumericValue = IsLifetime ? 0 : value;
    }

    private IntervalValue()
    {
        IsLifetime = true;
        NumericValue = 0;
    }

    public string Unit { get; } = null!;

    public int NumericValue { get; }

    public bool IsLifetime { get; }

    public static IntervalValue Parse(string input)
    {
        if (input.Equals(IntervalLifetimeToken, StringComparison.OrdinalIgnoreCase))
        {
            return new IntervalValue();
        }

        bool isMatch = IntervalRegex.IsMatch(input);

        if (!isMatch)
        {
            throw new ArgumentException("Invalid interval");
        }

        MatchCollection matches = IntervalRegex.Matches(input);

        int value = int.Parse(matches[0].Groups[1].Value);
        string unit = matches[0].Groups[2].Value.ToLower();

        try
        {
            var args = new IntervalValue(value, unit);

            return args;
        }
        catch
        {
            throw new ArgumentException("Invalid interval");
        }
    }
}
