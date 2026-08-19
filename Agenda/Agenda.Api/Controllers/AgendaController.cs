using Agenda.Api.Contracts;
using Agenda.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace Agenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgendaController(
    AddEventToAgendaUseCase addEventToAgendaUseCase,
    GetAgendaUseCase getAgendaUseCase) : ControllerBase
{
    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetAgenda(int userId, CancellationToken ct)
    {
        if (userId <= 0)
            return BadRequest("userId must be greater than zero.");

        var agendaItems = await getAgendaUseCase.ExecuteAsync(userId, ct);
        return Ok(agendaItems.Select(AgendaItemResponse.FromDomain));
    }

    [HttpPost("{userId:int}/events")]
    public async Task<IActionResult> AddEventToAgenda(int userId, [FromBody] AddEventToAgendaRequest request, CancellationToken ct)
    {
        try
        {
            var result = await addEventToAgendaUseCase.ExecuteAsync(userId, request.EventId, ct);
            var response = AgendaItemResponse.FromDomain(result.AgendaItem);

            return result.Created
                ? CreatedAtAction(nameof(GetAgenda), new { userId }, response)
                : Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
