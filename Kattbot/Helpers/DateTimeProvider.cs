using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Kattbot.Helpers;

public class DateTimeProvider
{
    private readonly ILogger<DateTimeProvider> _logger;
    private readonly TimeZoneInfo _norwayTimeZone;

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

        TimeSpan offset = _norwayTimeZone.GetUtcOffset(currentDateTimeUtc);

        _logger.LogDebug($"offset: {offset}");

        DateTime norwayCurrentLocalTime = currentDateTimeUtc.Add(TimeSpan.FromMilliseconds(offset.TotalMilliseconds));

        _logger.LogDebug($"norwayCurrentLocalTime: {norwayCurrentLocalTime}");

        var norwayCurrentDateTimeOffset = new DateTimeOffset(norwayCurrentLocalTime, offset);

        _logger.LogDebug($"norwayCurrentDateTimeOffset: {norwayCurrentDateTimeOffset}");

        return norwayCurrentDateTimeOffset;
    }

    public DateTimeOffset ConvertDateTimeUtcToNorway(DateTime dateTimeUtc)
    {
        TimeSpan offset = _norwayTimeZone.GetUtcOffset(dateTimeUtc);

        DateTime dateTimeOffsetAsLocal = DateTime.SpecifyKind(dateTimeUtc, DateTimeKind.Unspecified);

        DateTime norwayLocalDateTime = dateTimeOffsetAsLocal.Add(TimeSpan.FromMilliseconds(offset.TotalMilliseconds));

        var norwayCurrentDateTimeOffset = new DateTimeOffset(norwayLocalDateTime, offset);

        return norwayCurrentDateTimeOffset;
    }

    public DateTimeOffset ParseAsNorwayDateTimeOffset(string dateTimeString)
    {
        _logger.LogDebug("ParseAsNorwayDateTimeOffset");
        _logger.LogDebug($"dateTimeString: {dateTimeString}");

        DateTime dateTimeAsLocal = DateTime.Parse(dateTimeString);

        _logger.LogDebug($"dateTimeAsLocal: {dateTimeAsLocal}");

        TimeSpan offset = _norwayTimeZone.GetUtcOffset(dateTimeAsLocal);

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
