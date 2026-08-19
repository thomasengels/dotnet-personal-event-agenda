using Bff.Domain.Models;
using Bff.Domain.Services;

namespace Bff.Application.UseCases;

public sealed class GetUserAgendaUseCase(IAgendaClient agendaClient, IEventClient eventClient, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AgendaEntry>> ExecuteAsync(
        int userId, DateTime? referenceDate, AgendaTimeframe timeframe, CancellationToken ct)
    {
        var agendaItems = await agendaClient.GetAgendaAsync(userId, ct);
        if (agendaItems.Count == 0)
            return [];

        var window = AgendaWindow.For(referenceDate ?? timeProvider.GetUtcNow().UtcDateTime, timeframe);
        var events = await eventClient.GetEventsAsync(window.Start, window.End, ct);

        var agendaItemIdByEventId = agendaItems
            .GroupBy(item => item.EventId)
            .ToDictionary(group => group.Key, group => group.First().Id);

        return events
            .Where(@event => agendaItemIdByEventId.ContainsKey(@event.Id))
            .OrderBy(@event => @event.StartDate)
            .Select(@event => new AgendaEntry(agendaItemIdByEventId[@event.Id], @event))
            .ToList();
    }
}
