namespace Bff.Domain.Models;

public readonly record struct AgendaWindow(DateTime Start, DateTime End)
{
    public static AgendaWindow For(DateTime referenceDate, AgendaTimeframe timeframe)
    {
        var day = referenceDate.Date;

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
        var start = new DateTime(day.Year, day.Month, 1, 0, 0, 0, day.Kind);

        return new AgendaWindow(start, start.AddMonths(1));
    }
}
