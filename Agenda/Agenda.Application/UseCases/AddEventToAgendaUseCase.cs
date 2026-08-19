using Agenda.Domain.Models;
using Agenda.Domain.Ports;

namespace Agenda.Application.UseCases;

public sealed class AddEventToAgendaUseCase(IAgendaRepository agendaRepository, TimeProvider timeProvider)
{
    public Task<AddAgendaItemResult> ExecuteAsync(int userId, Guid eventId, CancellationToken ct)
    {
        var agendaItem = AgendaItem.CreateNew(userId, eventId, timeProvider.GetUtcNow().UtcDateTime);
        return agendaRepository.AddEventAsync(agendaItem, ct);
    }
}
