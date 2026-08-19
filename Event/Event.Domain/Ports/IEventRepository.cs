namespace Event.Domain.Ports;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Event>> GetAllAsync(DateTime start, DateTime? end, CancellationToken ct);
    Task AddAsync(Event @event, CancellationToken ct);
}
