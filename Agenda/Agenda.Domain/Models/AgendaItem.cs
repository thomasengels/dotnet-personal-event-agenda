namespace Agenda.Domain.Models;

public sealed record AgendaItem(int Id, string Title, DateTime StartUtc, DateTime EndUtc);