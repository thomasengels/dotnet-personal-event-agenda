using Bff.Domain.Models;

namespace Bff.Domain.Services;

public interface IAgendaClient
{
    Task<AgendaItemSummary> AddEventToAgendaAsync(int userId, Guid eventId, CancellationToken ct);
}
