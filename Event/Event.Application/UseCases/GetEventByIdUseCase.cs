using Event.Domain.Ports;

namespace Event.Application.UseCases;

public sealed class GetEventByIdUseCase(IEventRepository eventRepository)
{
    public Task<DomainEvent?> ExecuteAsync(Guid id, CancellationToken ct) => eventRepository.GetByIdAsync(id, ct);
}
