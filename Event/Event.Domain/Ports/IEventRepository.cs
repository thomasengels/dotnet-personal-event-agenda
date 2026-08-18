namespace Event.Domain.Ports;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Event @event, CancellationToken ct);
}
