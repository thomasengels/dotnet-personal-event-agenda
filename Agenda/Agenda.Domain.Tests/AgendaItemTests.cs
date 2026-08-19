using Agenda.Domain.Models;
using Xunit;

namespace Agenda.Domain.Tests;

public sealed class AgendaItemTests
{
    [Fact]
    public void CreateNew_WithValidValues_CreatesAgendaItem()
    {
        var eventId = Guid.NewGuid();
        var createdUtc = DateTime.UtcNow;

        var item = AgendaItem.CreateNew(1, eventId, createdUtc);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(1, item.UserId);
        Assert.Equal(eventId, item.EventId);
        Assert.Equal(createdUtc, item.CreatedUtc);
    }

    [Fact]
    public void CreateNew_WithEmptyEventId_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            AgendaItem.CreateNew(1, Guid.Empty, DateTime.UtcNow));

        Assert.Equal("eventId", ex.ParamName);
    }

    [Fact]
    public void CreateNew_WithInvalidUserId_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            AgendaItem.CreateNew(0, Guid.NewGuid(), DateTime.UtcNow));

        Assert.Equal("userId", ex.ParamName);
    }
}
