using Bff.Api.Contracts;
using Bff.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace Bff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(IEventClient eventClient, IAgendaClient agendaClient) : ControllerBase
{
    [HttpGet("{userId:int}")]
    public IActionResult GetDashboard(int userId)
    {
        return Ok(new[]
        {
            $"Dashboard for user {userId}",
            "Upcoming events",
            "Pending invitations"
        });
    }

    [HttpPost("{userId:int}/agenda/events/{eventId:guid}")]
    public async Task<IActionResult> AddSelectedEventToAgenda(int userId, Guid eventId, CancellationToken ct)
    {
        if (userId <= 0)
            return BadRequest("userId must be greater than zero.");

        try
        {
            if (await eventClient.GetEventByIdAsync(eventId, ct) is null)
                return NotFound();

            var agendaItem = await agendaClient.AddEventToAgendaAsync(userId, eventId, ct);
            return Ok(AddSelectedEventToAgendaResponse.FromDomain(agendaItem));
        }
        catch (DownstreamServiceUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
        }
    }
}
