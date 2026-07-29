namespace ScarletPigsServices.Api.Services.Addon;

/// <summary>
/// The weekly operation window: Sunday 13:00-19:00 Swedish/German wall-clock time.
/// </summary>
/// <remarks>
/// Timestamps are stored as UTC and stay offset-agnostic; only this gate converts.
/// Converting to the zone gives wall-clock local time, so the window is the same clock
/// reading whether CET or CEST is observed - a fixed UTC offset would drift by an hour
/// across the daylight saving boundary.
/// </remarks>
public static class SessionWindow
{
    private static readonly TimeOnly WindowStart = new(13, 0);
    private static readonly TimeOnly WindowEnd = new(19, 0);
    private static readonly TimeZoneInfo SessionTimeZone = ResolveTimeZone();

    /// <summary>
    /// Determines whether <paramref name="instant"/> falls inside the operation window and,
    /// if so, yields the local date of that session (always a Sunday).
    /// </summary>
    public static bool TryGetSessionDate(DateTimeOffset instant, out DateOnly sessionDate)
    {
        var local = TimeZoneInfo.ConvertTime(instant, SessionTimeZone);
        sessionDate = DateOnly.FromDateTime(local.DateTime);

        if (local.DayOfWeek != DayOfWeek.Sunday)
        {
            return false;
        }

        var time = TimeOnly.FromDateTime(local.DateTime);
        return time >= WindowStart && time < WindowEnd;
    }

    private static TimeZoneInfo ResolveTimeZone()
    {
        // The IANA id needs tzdata present; fall back to the Windows id for the same zone.
        foreach (var id in (string[])["Europe/Stockholm", "W. Europe Standard Time"])
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Try the next id.
            }
        }

        throw new InvalidOperationException(
            "Neither 'Europe/Stockholm' nor 'W. Europe Standard Time' is available on this system.");
    }
}
