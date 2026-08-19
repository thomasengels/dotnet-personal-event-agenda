using Agenda.Application.UseCases;
using Agenda.Domain.Models;
using Agenda.Domain.Ports;
using Xunit;

namespace Agenda.Application.Tests;

public sealed class AddEventToAgendaUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesAgendaItemWithCurrentTime()
    {
        var now = new DateTimeOffset(2026, 8, 19, 10, 30, 0, TimeSpan.Zero);
        var repository = new StubAgendaRepository();
        var useCase = new AddEventToAgendaUseCase(repository, new FakeTimeProvider(now));
        var eventId = Guid.NewGuid();

        var result = await useCase.ExecuteAsync(1, eventId, CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal(1, result.AgendaItem.UserId);
        Assert.Equal(eventId, result.AgendaItem.EventId);
        Assert.Equal(now.UtcDateTime, result.AgendaItem.CreatedUtc);
    }

    private sealed class StubAgendaRepository : IAgendaRepository
    {
        public Task<IReadOnlyList<AgendaItem>> GetByUserIdAsync(int userId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AgendaItem>>([]);

        public Task<AddAgendaItemResult> AddEventAsync(AgendaItem agendaItem, CancellationToken ct) =>
            Task.FromResult(new AddAgendaItemResult(agendaItem, Created: true));
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
