using Agenda.Domain.Models;
using Agenda.Domain.Ports;

namespace Agenda.Application.UseCases;

public sealed class GetAgendaUseCase(IAgendaRepository agendaRepository)
{
    public Task<IReadOnlyList<AgendaItem>> ExecuteAsync(int userId, CancellationToken ct) =>
        agendaRepository.GetByUserIdAsync(userId, ct);
}
