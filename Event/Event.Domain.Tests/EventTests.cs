namespace Event.Domain.Tests;

public class EventTests
{
    private static Address ValidAddress() => new("Main St 1", "Ghent", "9000", "Belgium");

    [Fact]
    public void CreateNew_WithValidInput_CreatesEvent()
    {
        var start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var @event = Event.CreateNew("Conference", "A conference.", ValidAddress(), start, end);

        Assert.NotEqual(Guid.Empty, @event.Id);
        Assert.Equal("Conference", @event.Name);
        Assert.Equal("A conference.", @event.Description);
        Assert.Equal(ValidAddress(), @event.Location);
        Assert.Equal(start, @event.StartDate);
        Assert.Equal(end, @event.EndDate);
    }

    [Fact]
    public void CreateNew_WithNullDescription_CreatesEvent()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);

        var @event = Event.CreateNew("Conference", null, ValidAddress(), start, end);

        Assert.Null(@event.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateNew_WithInvalidName_Throws(string? name)
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);

        Assert.Throws<ArgumentException>(() => Event.CreateNew(name!, null, ValidAddress(), start, end));
    }

    [Fact]
    public void CreateNew_WithDescriptionOver255Characters_Throws()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        var description = new string('a', 256);

        Assert.Throws<ArgumentException>(() => Event.CreateNew("Conference", description, ValidAddress(), start, end));
    }

    [Fact]
    public void CreateNew_WithDescriptionOf255Characters_CreatesEvent()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);
        var description = new string('a', 255);

        var @event = Event.CreateNew("Conference", description, ValidAddress(), start, end);

        Assert.Equal(description, @event.Description);
    }

    [Fact]
    public void CreateNew_WithEndDateBeforeStartDate_Throws()
    {
        var start = DateTime.UtcNow;
        var end = start.AddHours(-1);

        Assert.Throws<ArgumentException>(() => Event.CreateNew("Conference", null, ValidAddress(), start, end));
    }

    [Fact]
    public void CreateNew_WithEndDateEqualToStartDate_Throws()
    {
        var start = DateTime.UtcNow;

        Assert.Throws<ArgumentException>(() => Event.CreateNew("Conference", null, ValidAddress(), start, start));
    }

    [Fact]
    public void Reconstitute_PreservesTheGivenId()
    {
        var id = Guid.NewGuid();
        var start = DateTime.UtcNow;
        var end = start.AddHours(1);

        var @event = Event.Reconstitute(id, "Conference", null, ValidAddress(), start, end);

        Assert.Equal(id, @event.Id);
    }
}
