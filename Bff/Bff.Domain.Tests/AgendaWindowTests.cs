using Bff.Domain.Models;
using Xunit;

namespace Bff.Domain.Tests;

public sealed class AgendaWindowTests
{
    [Fact]
    public void For_Day_ReturnsMidnightToMidnightNextDay()
    {
        var reference = new DateTime(2026, 8, 19, 14, 30, 0);

        var window = AgendaWindow.For(reference, AgendaTimeframe.Day);

        Assert.Equal(new DateTime(2026, 8, 19), window.Start);
        Assert.Equal(new DateTime(2026, 8, 20), window.End);
    }

    [Fact]
    public void For_Week_ReturnsMondayToNextMonday()
    {
        // 2026-08-19 is a Wednesday
        var reference = new DateTime(2026, 8, 19, 14, 30, 0);

        var window = AgendaWindow.For(reference, AgendaTimeframe.Week);

        Assert.Equal(new DateTime(2026, 8, 17), window.Start);
        Assert.Equal(new DateTime(2026, 8, 24), window.End);
    }

    [Fact]
    public void For_Week_OnMonday_StartsOnThatDay()
    {
        var reference = new DateTime(2026, 8, 17); // Monday

        var window = AgendaWindow.For(reference, AgendaTimeframe.Week);

        Assert.Equal(new DateTime(2026, 8, 17), window.Start);
        Assert.Equal(new DateTime(2026, 8, 24), window.End);
    }

    [Fact]
    public void For_Week_SpanningYearBoundary_CrossesIntoNextYear()
    {
        // 2026-12-29 is a Tuesday; the week starts Monday 2026-12-28 and ends 2027-01-04
        var reference = new DateTime(2026, 12, 29);

        var window = AgendaWindow.For(reference, AgendaTimeframe.Week);

        Assert.Equal(new DateTime(2026, 12, 28), window.Start);
        Assert.Equal(new DateTime(2027, 1, 4), window.End);
    }

    [Fact]
    public void For_Month_ReturnsFirstOfMonthToFirstOfNextMonth()
    {
        var reference = new DateTime(2026, 8, 19);

        var window = AgendaWindow.For(reference, AgendaTimeframe.Month);

        Assert.Equal(new DateTime(2026, 8, 1), window.Start);
        Assert.Equal(new DateTime(2026, 9, 1), window.End);
    }

    [Fact]
    public void For_Month_December_RollsOverIntoNextYear()
    {
        var reference = new DateTime(2026, 12, 15);

        var window = AgendaWindow.For(reference, AgendaTimeframe.Month);

        Assert.Equal(new DateTime(2026, 12, 1), window.Start);
        Assert.Equal(new DateTime(2027, 1, 1), window.End);
    }

    [Fact]
    public void For_UnspecifiedKindReferenceDate_ReturnsUtcKindBoundaries()
    {
        // Downstream calls (e.g. to the Event API) require UTC-kind DateTimes; an
        // unspecified-kind reference date (e.g. parsed from a plain "date" query
        // string) must not leak through as Unspecified.
        var reference = DateTime.SpecifyKind(new DateTime(2026, 8, 19), DateTimeKind.Unspecified);

        var window = AgendaWindow.For(reference, AgendaTimeframe.Day);

        Assert.Equal(DateTimeKind.Utc, window.Start.Kind);
        Assert.Equal(DateTimeKind.Utc, window.End.Kind);
    }
}
