using Bff.Domain.Models;

namespace Bff.Api.Contracts;

public sealed record AddSelectedEventToAgendaResponse(Guid Id, int UserId, Guid EventId, DateTime CreatedUtc)
{
    public static AddSelectedEventToAgendaResponse FromDomain(AgendaItemSummary agendaItem) => new(
        agendaItem.Id,
        agendaItem.UserId,
        agendaItem.EventId,
        agendaItem.CreatedUtc);
}
