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
}
