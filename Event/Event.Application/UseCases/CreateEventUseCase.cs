using Event.Domain;
using Event.Domain.Ports;

namespace Event.Application.UseCases;

public sealed class CreateEventUseCase(IEventRepository eventRepository)
{
    public async Task<DomainEvent> ExecuteAsync(
        string name,
        string? description,
        Address location,
        DateTime startDate,
        DateTime endDate,
        CancellationToken ct)
    {
        var @event = DomainEvent.CreateNew(name, description, location, startDate, endDate);
        await eventRepository.AddAsync(@event, ct);
        return @event;
    }
}
