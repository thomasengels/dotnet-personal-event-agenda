using System.Net;
using System.Net.Http.Json;
using Bff.Domain.Models;
using Bff.Domain.Services;

namespace Bff.Api.Services;

public sealed class EventClient(HttpClient httpClient) : IEventClient
{
    public async Task<EventSummary?> GetEventByIdAsync(Guid eventId, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync($"/api/events/{eventId}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventSummary>(cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            throw new DownstreamServiceUnavailableException("Event", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new DownstreamServiceUnavailableException("Event", ex);
        }
    }

    public async Task<IReadOnlyList<EventSummary>> GetEventsAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        try
        {
            var query = $"startDate={Uri.EscapeDataString(startDate.ToString("o"))}&endDate={Uri.EscapeDataString(endDate.ToString("o"))}";
            using var response = await httpClient.GetAsync($"/api/events?{query}", ct);
            response.EnsureSuccessStatusCode();

            var events = await response.Content.ReadFromJsonAsync<List<EventSummary>>(cancellationToken: ct);
            return events ?? [];
        }
        catch (HttpRequestException ex)
        {
            throw new DownstreamServiceUnavailableException("Event", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new DownstreamServiceUnavailableException("Event", ex);
        }
    }
}
