using Bff.Domain.Models;

namespace Bff.Api.Contracts;

public sealed record AgendaEntryResponse(
    Guid AgendaItemId,
    Guid EventId,
    string Name,
    string? Description,
    string Street,
    string City,
    string PostalCode,
    string Country,
    DateTime StartDate,
    DateTime EndDate)
{
    public static AgendaEntryResponse FromDomain(AgendaEntry entry) => new(
        entry.AgendaItemId,
        entry.Event.Id,
        entry.Event.Name,
        entry.Event.Description,
        entry.Event.Street,
        entry.Event.City,
        entry.Event.PostalCode,
        entry.Event.Country,
        entry.Event.StartDate,
        entry.Event.EndDate);
}
