namespace Bff.Domain.Models;

public readonly record struct AgendaWindow(DateTime Start, DateTime End)
{
    public static AgendaWindow For(DateTime referenceDate, AgendaTimeframe timeframe)
    {
        // Window boundaries are always UTC-kind, matching this system's convention of
        // storing/exchanging DateTimes as UTC (e.g. AgendaItem.CreatedUtc) - callers may
        // pass an unspecified-kind date (e.g. a calendar day parsed from a query string).
        var day = DateTime.SpecifyKind(referenceDate.Date, DateTimeKind.Utc);

        return timeframe switch
        {
            AgendaTimeframe.Day => new AgendaWindow(day, day.AddDays(1)),
            AgendaTimeframe.Week => ForWeek(day),
            AgendaTimeframe.Month => ForMonth(day),
            _ => throw new ArgumentOutOfRangeException(nameof(timeframe), timeframe, "Unsupported timeframe.")
        };
    }

    private static AgendaWindow ForWeek(DateTime day)
    {
        var daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
        var start = day.AddDays(-daysSinceMonday);

        return new AgendaWindow(start, start.AddDays(7));
    }

    private static AgendaWindow ForMonth(DateTime day)
    {
        var start = new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return new AgendaWindow(start, start.AddMonths(1));
    }
}
