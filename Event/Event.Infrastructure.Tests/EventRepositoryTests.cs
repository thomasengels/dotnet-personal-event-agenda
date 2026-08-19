using Event.Domain;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Event.Infrastructure.Tests;

public class EventRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private EventDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var optionsBuilder = new DbContextOptionsBuilder<EventDbContext>()
            .UseNpgsql(_postgres.GetConnectionString());
        _dbContext = new EventDbContext(optionsBuilder.Options);
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsTheEvent()
    {
        var repository = new EventRepository(_dbContext);
        var location = new Address("Main St 1", "Ghent", "9000", "Belgium");
        var start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var @event = DomainEvent.CreateNew("Conference", "A conference.", location, start, end);

        await repository.AddAsync(@event, CancellationToken.None);
        var fetched = await repository.GetByIdAsync(@event.Id, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(@event.Id, fetched.Id);
        Assert.Equal(@event.Name, fetched.Name);
        Assert.Equal(@event.Description, fetched.Description);
        Assert.Equal(@event.Location, fetched.Location);
        Assert.Equal(@event.StartDate, fetched.StartDate);
        Assert.Equal(@event.EndDate, fetched.EndDate);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var repository = new EventRepository(_dbContext);

        var fetched = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesEventThatEndsBeforeTheWindowStarts()
    {
        var repository = new EventRepository(_dbContext);
        var window = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var @event = await CreateEventAsync(repository, window.AddDays(-2), window.AddDays(-1));

        var results = await repository.GetAllAsync(window, null, CancellationToken.None);

        Assert.DoesNotContain(results, e => e.Id == @event.Id);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesEventThatAlreadyEnded()
    {
        var repository = new EventRepository(_dbContext);
        var now = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var @event = await CreateEventAsync(repository, now.AddHours(-3), now.AddHours(-1));

        var results = await repository.GetAllAsync(now, null, CancellationToken.None);

        Assert.DoesNotContain(results, e => e.Id == @event.Id);
    }

    [Fact]
    public async Task GetAllAsync_IncludesEventThatOverlapsTheWindowStartBoundary()
    {
        var repository = new EventRepository(_dbContext);
        var window = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var @event = await CreateEventAsync(repository, window.AddHours(-1), window.AddHours(1));

        var results = await repository.GetAllAsync(window, null, CancellationToken.None);

        Assert.Contains(results, e => e.Id == @event.Id);
    }

    [Fact]
    public async Task GetAllAsync_IncludesEventThatOverlapsTheWindowEndBoundary()
    {
        var repository = new EventRepository(_dbContext);
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var @event = await CreateEventAsync(repository, end.AddHours(-1), end.AddHours(1));

        var results = await repository.GetAllAsync(start, end, CancellationToken.None);

        Assert.Contains(results, e => e.Id == @event.Id);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesEventThatStartsAtOrAfterTheWindowEnd()
    {
        var repository = new EventRepository(_dbContext);
        var start = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var @event = await CreateEventAsync(repository, end, end.AddHours(1));

        var results = await repository.GetAllAsync(start, end, CancellationToken.None);

        Assert.DoesNotContain(results, e => e.Id == @event.Id);
    }

    [Fact]
    public async Task GetAllAsync_WithNoEndDate_IncludesEventFarInTheFuture()
    {
        var repository = new EventRepository(_dbContext);
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var @event = await CreateEventAsync(repository, now.AddYears(10), now.AddYears(10).AddHours(1));

        var results = await repository.GetAllAsync(now, null, CancellationToken.None);

        Assert.Contains(results, e => e.Id == @event.Id);
    }

    [Fact]
    public async Task GetAllAsync_OrdersResultsByStartDateAscending()
    {
        var repository = new EventRepository(_dbContext);
        var now = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var later = await CreateEventAsync(repository, now.AddDays(2), now.AddDays(2).AddHours(1));
        var earlier = await CreateEventAsync(repository, now.AddDays(1), now.AddDays(1).AddHours(1));

        var results = await repository.GetAllAsync(now, null, CancellationToken.None);

        var ids = results.Select(e => e.Id).ToList();
        Assert.True(ids.IndexOf(earlier.Id) < ids.IndexOf(later.Id));
    }

    private static async Task<DomainEvent> CreateEventAsync(EventRepository repository, DateTime start, DateTime end)
    {
        var location = new Address("Main St 1", "Ghent", "9000", "Belgium");
        var @event = DomainEvent.CreateNew(Guid.NewGuid().ToString(), null, location, start, end);

        await repository.AddAsync(@event, CancellationToken.None);
        return @event;
    }
}
