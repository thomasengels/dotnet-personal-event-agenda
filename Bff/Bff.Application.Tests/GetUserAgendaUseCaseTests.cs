using Bff.Application.UseCases;
using Bff.Domain.Models;
using Bff.Domain.Services;
using Xunit;

namespace Bff.Application.Tests;

public sealed class GetUserAgendaUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsAgendaEventsSortedByStartDate()
    {
        var eventA = CreateEvent(new DateTime(2026, 8, 19, 9, 0, 0));
        var eventB = CreateEvent(new DateTime(2026, 8, 19, 14, 0, 0));
        var eventNotOnAgenda = CreateEvent(new DateTime(2026, 8, 19, 8, 0, 0));

        var agendaItemA = new AgendaItemSummary(Guid.NewGuid(), 1, eventA.Id, DateTime.UtcNow);
        var agendaItemB = new AgendaItemSummary(Guid.NewGuid(), 1, eventB.Id, DateTime.UtcNow);

        var agendaClient = new StubAgendaClient([agendaItemB, agendaItemA]);
        var eventClient = new StubEventClient([eventB, eventA, eventNotOnAgenda]);
        var useCase = new GetUserAgendaUseCase(agendaClient, eventClient, new FakeTimeProvider(DateTimeOffset.UtcNow));

        var result = await useCase.ExecuteAsync(1, new DateTime(2026, 8, 19), AgendaTimeframe.Day, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(agendaItemA.Id, result[0].AgendaItemId);
        Assert.Equal(eventA.Id, result[0].Event.Id);
        Assert.Equal(agendaItemB.Id, result[1].AgendaItemId);
        Assert.Equal(eventB.Id, result[1].Event.Id);
    }

    [Fact]
    public async Task ExecuteAsync_AgendaEventNotInWindow_IsExcluded()
    {
        var agendaItem = new AgendaItemSummary(Guid.NewGuid(), 1, Guid.NewGuid(), DateTime.UtcNow);
        var agendaClient = new StubAgendaClient([agendaItem]);
        var eventClient = new StubEventClient([]); // the Event API already filtered this event out of the window

        var useCase = new GetUserAgendaUseCase(agendaClient, eventClient, new FakeTimeProvider(DateTimeOffset.UtcNow));

        var result = await useCase.ExecuteAsync(1, new DateTime(2026, 8, 19), AgendaTimeframe.Day, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyAgenda_DoesNotCallEventClient()
    {
        var agendaClient = new StubAgendaClient([]);
        var eventClient = new StubEventClient([]);
        var useCase = new GetUserAgendaUseCase(agendaClient, eventClient, new FakeTimeProvider(DateTimeOffset.UtcNow));

        var result = await useCase.ExecuteAsync(1, new DateTime(2026, 8, 19), AgendaTimeframe.Day, CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(0, eventClient.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_NoReferenceDate_UsesCurrentTimeFromTimeProvider()
    {
        var now = new DateTimeOffset(2026, 8, 19, 10, 0, 0, TimeSpan.Zero);
        var eventId = Guid.NewGuid();
        var agendaItem = new AgendaItemSummary(Guid.NewGuid(), 1, eventId, DateTime.UtcNow);
        var @event = CreateEvent(new DateTime(2026, 8, 19, 12, 0, 0), eventId);

        var agendaClient = new StubAgendaClient([agendaItem]);
        var eventClient = new StubEventClient([@event]);
        var useCase = new GetUserAgendaUseCase(agendaClient, eventClient, new FakeTimeProvider(now));

        await useCase.ExecuteAsync(1, null, AgendaTimeframe.Day, CancellationToken.None);

        Assert.Equal(new DateTime(2026, 8, 19), eventClient.LastStartDate);
        Assert.Equal(new DateTime(2026, 8, 20), eventClient.LastEndDate);
    }

    private static EventSummary CreateEvent(DateTime startDate, Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        "Event",
        null,
        "Street",
        "City",
        "0000",
        "Country",
        startDate,
        startDate.AddHours(1));

    private sealed class StubAgendaClient(IReadOnlyList<AgendaItemSummary> agendaItems) : IAgendaClient
    {
        public Task<IReadOnlyList<AgendaItemSummary>> GetAgendaAsync(int userId, CancellationToken ct) =>
            Task.FromResult(agendaItems);

        public Task<AgendaItemSummary> AddEventToAgendaAsync(int userId, Guid eventId, CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class StubEventClient(IReadOnlyList<EventSummary> events) : IEventClient
    {
        public int CallCount { get; private set; }
        public DateTime LastStartDate { get; private set; }
        public DateTime LastEndDate { get; private set; }

        public Task<EventSummary?> GetEventByIdAsync(Guid eventId, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EventSummary>> GetEventsAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
        {
            CallCount++;
            LastStartDate = startDate;
            LastEndDate = endDate;
            return Task.FromResult(events);
        }
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
