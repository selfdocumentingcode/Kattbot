using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Kattbot.Helpers
{
    public class DateTimeProvider
    {
        private readonly TimeZoneInfo _norwayTimeZone;
        private readonly ILogger<DateTimeProvider> _logger;

        public DateTimeProvider(ILogger<DateTimeProvider> logger)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _norwayTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            }
            else
            {
                _norwayTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
            }
            _logger = logger;
        }

        public DateTime GetCurrentUtcDateTime()
        {
            return DateTime.UtcNow;
        }

        public DateTimeOffset GetCurrentNorwayDateTimeOffset()
        {
            _logger.LogDebug("GetCurrentNorwayDateTimeOffset");

            var currentDateTimeUtc = new DateTime(DateTime.UtcNow.Ticks);

            _logger.LogDebug($"currentDateTimeUtc: {currentDateTimeUtc}");

            var offset = _norwayTimeZone.GetUtcOffset(currentDateTimeUtc);

            _logger.LogDebug($"offset: {offset}");

            var norwayCurrentLocalTime = currentDateTimeUtc.Add(TimeSpan.FromMilliseconds(offset.TotalMilliseconds));

            _logger.LogDebug($"norwayCurrentLocalTime: {norwayCurrentLocalTime}");

            var norwayCurrentDateTimeOffset = new DateTimeOffset(norwayCurrentLocalTime, offset);

            _logger.LogDebug($"norwayCurrentDateTimeOffset: {norwayCurrentDateTimeOffset}");

            return norwayCurrentDateTimeOffset;
        }

        public DateTimeOffset ConvertDateTimeUtcToNorway(DateTime dateTimeUtc)
        {
            var offset = _norwayTimeZone.GetUtcOffset(dateTimeUtc);

            var dateTimeOffsetAsLocal = DateTime.SpecifyKind(dateTimeUtc, DateTimeKind.Unspecified);

            var norwayLocalDateTime = dateTimeOffsetAsLocal.Add(TimeSpan.FromMilliseconds(offset.TotalMilliseconds));

            var norwayCurrentDateTimeOffset = new DateTimeOffset(norwayLocalDateTime, offset);

            return norwayCurrentDateTimeOffset;
        }

        public DateTimeOffset ParseAsNorwayDateTimeOffset(string dateTimeString)
        {
            _logger.LogDebug("ParseAsNorwayDateTimeOffset");
            _logger.LogDebug($"dateTimeString: {dateTimeString}");

            var dateTimeAsLocal = DateTime.Parse(dateTimeString);

            _logger.LogDebug($"dateTimeAsLocal: {dateTimeAsLocal}");

            var offset = _norwayTimeZone.GetUtcOffset(dateTimeAsLocal);

            _logger.LogDebug($"offset: {offset}");

            var dateTimeOffset = new DateTimeOffset(dateTimeAsLocal, offset);

            _logger.LogDebug($"dateTimeOffset: {dateTimeOffset}");

            return dateTimeOffset;
        }

        public string FormatDateTimeOffsetToIso(DateTimeOffset dateTimeOffset)
        {
            return $"{dateTimeOffset:yyyy-MM-dd HH:mm}";
        }
    }
}
