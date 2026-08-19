using Agenda.Domain.Models;

namespace Agenda.Domain.Ports;

public interface IAgendaRepository
{
    Task<IReadOnlyList<AgendaItem>> GetByUserIdAsync(int userId, CancellationToken ct);

    Task<AddAgendaItemResult> AddEventAsync(AgendaItem agendaItem, CancellationToken ct);
}
