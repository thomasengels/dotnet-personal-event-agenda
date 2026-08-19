using Event.Domain.Ports;

namespace Event.Application.UseCases;

public sealed class GetEventsUseCase(IEventRepository eventRepository, TimeProvider timeProvider)
{
    public Task<IReadOnlyList<DomainEvent>> ExecuteAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var effectiveStart = startDate is null ? now : Max(startDate.Value, now);

        return eventRepository.GetAllAsync(effectiveStart, endDate, ct);
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}
