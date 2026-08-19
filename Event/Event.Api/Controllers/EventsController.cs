using Event.Api.Contracts;
using Event.Application.UseCases;
using Event.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Event.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController(
    CreateEventUseCase createEventUseCase,
    GetEventByIdUseCase getEventByIdUseCase,
    GetEventsUseCase getEventsUseCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEvents([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, CancellationToken ct)
    {
        if (endDate is not null && startDate is not null && endDate < startDate)
            return BadRequest("endDate must not be before startDate.");

        var events = await getEventsUseCase.ExecuteAsync(startDate, endDate, ct);
        return Ok(events.Select(EventResponse.FromDomain));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEventById(Guid id, CancellationToken ct)
    {
        var @event = await getEventByIdUseCase.ExecuteAsync(id, ct);
        return @event is null ? NotFound() : Ok(EventResponse.FromDomain(@event));
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        try
        {
            var location = new Address(request.Street, request.City, request.PostalCode, request.Country);
            var @event = await createEventUseCase.ExecuteAsync(
                request.Name, request.Description, location, request.StartDate, request.EndDate, ct);

            return CreatedAtAction(nameof(GetEventById), new { id = @event.Id }, EventResponse.FromDomain(@event));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
