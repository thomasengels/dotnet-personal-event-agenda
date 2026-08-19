using Event.Application.UseCases;
using Event.Domain.Ports;

namespace Event.Application.Tests.UseCases;

public class GetEventsUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_WithNoFilters_QueriesFromNowWithNoUpperBound()
    {
        var repository = new FakeEventRepository();
        var useCase = new GetEventsUseCase(repository, new FixedTimeProvider(Now));

        await useCase.ExecuteAsync(startDate: null, endDate: null, CancellationToken.None);

        Assert.Equal(Now, repository.CapturedStart);
        Assert.Null(repository.CapturedEnd);
    }

    [Fact]
    public async Task ExecuteAsync_WithFutureStartDateOnly_PassesStartDateThrough()
    {
        var repository = new FakeEventRepository();
        var useCase = new GetEventsUseCase(repository, new FixedTimeProvider(Now));
        var futureStart = Now.AddDays(1);

        await useCase.ExecuteAsync(futureStart, endDate: null, CancellationToken.None);

        Assert.Equal(futureStart, repository.CapturedStart);
        Assert.Null(repository.CapturedEnd);
    }

    [Fact]
    public async Task ExecuteAsync_WithEndDateOnly_QueriesFromNowToEndDate()
    {
        var repository = new FakeEventRepository();
        var useCase = new GetEventsUseCase(repository, new FixedTimeProvider(Now));
        var end = Now.AddDays(7);

        await useCase.ExecuteAsync(startDate: null, end, CancellationToken.None);

        Assert.Equal(Now, repository.CapturedStart);
        Assert.Equal(end, repository.CapturedEnd);
    }

    [Fact]
    public async Task ExecuteAsync_WithPastStartDate_ClampsStartDateToNow()
    {
        var repository = new FakeEventRepository();
        var useCase = new GetEventsUseCase(repository, new FixedTimeProvider(Now));
        var pastStart = Now.AddDays(-1);

        await useCase.ExecuteAsync(pastStart, endDate: null, CancellationToken.None);

        Assert.Equal(Now, repository.CapturedStart);
    }

    [Fact]
    public async Task ExecuteAsync_WithFutureStartDateAndEndDate_PassesBothThrough()
    {
        var repository = new FakeEventRepository();
        var useCase = new GetEventsUseCase(repository, new FixedTimeProvider(Now));
        var start = Now.AddDays(1);
        var end = Now.AddDays(2);

        await useCase.ExecuteAsync(start, end, CancellationToken.None);

        Assert.Equal(start, repository.CapturedStart);
        Assert.Equal(end, repository.CapturedEnd);
    }

    private sealed class FixedTimeProvider(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
    }

    private sealed class FakeEventRepository : IEventRepository
    {
        public DateTime? CapturedStart { get; private set; }
        public DateTime? CapturedEnd { get; private set; }

        public Task<DomainEvent?> GetByIdAsync(Guid id, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DomainEvent>> GetAllAsync(DateTime start, DateTime? end, CancellationToken ct)
        {
            CapturedStart = start;
            CapturedEnd = end;
            return Task.FromResult<IReadOnlyList<DomainEvent>>([]);
        }

        public Task AddAsync(DomainEvent @event, CancellationToken ct) =>
            throw new NotSupportedException();
    }
}
