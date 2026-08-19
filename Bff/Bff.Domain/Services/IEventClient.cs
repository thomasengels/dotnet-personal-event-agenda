using Bff.Domain.Models;

namespace Bff.Domain.Services;

public interface IEventClient
{
    Task<EventSummary?> GetEventByIdAsync(Guid eventId, CancellationToken ct);

    Task<IReadOnlyList<EventSummary>> GetEventsAsync(DateTime startDate, DateTime endDate, CancellationToken ct);
}
