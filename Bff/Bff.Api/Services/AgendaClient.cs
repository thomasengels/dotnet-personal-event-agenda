using System.Net.Http.Json;
using Bff.Domain.Models;
using Bff.Domain.Services;

namespace Bff.Api.Services;

public sealed class AgendaClient(HttpClient httpClient) : IAgendaClient
{
    public async Task<AgendaItemSummary> AddEventToAgendaAsync(int userId, Guid eventId, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync($"/api/agenda/{userId}/events", new { eventId }, ct);
            response.EnsureSuccessStatusCode();

            var agendaItem = await response.Content.ReadFromJsonAsync<AgendaItemSummary>(cancellationToken: ct);
            return agendaItem ?? throw new DownstreamServiceUnavailableException("Agenda");
        }
        catch (HttpRequestException ex)
        {
            throw new DownstreamServiceUnavailableException("Agenda", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new DownstreamServiceUnavailableException("Agenda", ex);
        }
    }
}
