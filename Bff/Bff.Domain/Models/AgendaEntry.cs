namespace Bff.Domain.Models;

public sealed record AgendaEntry(Guid AgendaItemId, EventSummary Event);
