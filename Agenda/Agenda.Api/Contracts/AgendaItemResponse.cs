using Agenda.Domain.Models;

namespace Agenda.Api.Contracts;

public sealed record AgendaItemResponse(Guid Id, int UserId, Guid EventId, DateTime CreatedUtc)
{
    public static AgendaItemResponse FromDomain(AgendaItem agendaItem) => new(
        agendaItem.Id,
        agendaItem.UserId,
        agendaItem.EventId,
        agendaItem.CreatedUtc);
}
