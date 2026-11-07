namespace Bank.Application.Common;

/// <summary>
/// The calendar date in the bank's time zone. Servers run on UTC, but "today" for a Turkish customer
/// changes at midnight Istanbul time: at 01:00 local it is still yesterday in UTC.
/// </summary>
public static class BusinessDate
{
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static DateOnly Today(TimeProvider time)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), Istanbul).DateTime);
}
