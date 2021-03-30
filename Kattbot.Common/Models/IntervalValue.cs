using System;
using System.Text.RegularExpressions;

namespace Kattbot.Common.Models
{
    public class IntervalValue
    {
        public static readonly string IntervalLifetimeToken = "lifetime";
        private static Regex IntervalRegex = new Regex(@"^(\d+)(m|w|d)$", RegexOptions.IgnoreCase);

        public string Unit { get; } = null!;
        public int NumericValue { get; }

        public bool IsLifetime { get; private set; }

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

        public static IntervalValue Parse(string input)
        {
            if(input.Equals(IntervalLifetimeToken, StringComparison.OrdinalIgnoreCase))
            {
                return new IntervalValue();
            }

            var isMatch = IntervalRegex.IsMatch(input);

            if (!isMatch)
            {
                throw new ArgumentException("Invalid interval");
            }

            var matches = IntervalRegex.Matches(input);

            var value = int.Parse(matches[0].Groups[1].Value);
            var unit = matches[0].Groups[2].Value.ToLower();

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
}
