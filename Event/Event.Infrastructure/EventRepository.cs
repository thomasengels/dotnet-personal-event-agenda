using Event.Domain;
using Event.Domain.Ports;
using Event.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Event.Infrastructure;

public sealed class EventRepository(EventDbContext dbContext) : IEventRepository
{
    public async Task<DomainEvent?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await dbContext.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(DomainEvent @event, CancellationToken ct)
    {
        dbContext.Events.Add(ToEntity(@event));
        await dbContext.SaveChangesAsync(ct);
    }

    private static DomainEvent ToDomain(EventEntity entity) => DomainEvent.Reconstitute(
        entity.Id,
        entity.Name,
        entity.Description,
        new Address(entity.Location.Street, entity.Location.City, entity.Location.PostalCode, entity.Location.Country),
        entity.StartDate,
        entity.EndDate);

    private static EventEntity ToEntity(DomainEvent @event) => new()
    {
        Id = @event.Id,
        Name = @event.Name,
        Description = @event.Description,
        Location = new AddressEntity
        {
            Street = @event.Location.Street,
            City = @event.Location.City,
            PostalCode = @event.Location.PostalCode,
            Country = @event.Location.Country,
        },
        StartDate = @event.StartDate,
        EndDate = @event.EndDate,
    };
}
