namespace Bff.Domain.Models;

public sealed record AgendaItemSummary(Guid Id, int UserId, Guid EventId, DateTime CreatedUtc);
