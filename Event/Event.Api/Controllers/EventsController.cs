using Microsoft.AspNetCore.Mvc;

namespace Event.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetEvents()
    {
        return Ok(new[] { "Event1", "Event2" });
    }

    [HttpGet("{id:int}")]
    public IActionResult GetEventById(int id)
    {
        return Ok($"Event {id}");
    }

    [HttpPost]
    public IActionResult CreateEvent([FromBody] string eventName)
    {
        return CreatedAtAction(nameof(GetEventById), new { id = 1 }, eventName);
    }
}