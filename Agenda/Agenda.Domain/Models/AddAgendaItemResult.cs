namespace Agenda.Domain.Models;

public sealed record AddAgendaItemResult(AgendaItem AgendaItem, bool Created);
