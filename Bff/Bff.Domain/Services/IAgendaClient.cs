using Bff.Domain.Models;

namespace Bff.Domain.Services;

public interface IAgendaClient
{
    Task<IReadOnlyList<AgendaItemSummary>> GetAgendaAsync(int userId, CancellationToken ct);

    Task<AgendaItemSummary> AddEventToAgendaAsync(int userId, Guid eventId, CancellationToken ct);
}
