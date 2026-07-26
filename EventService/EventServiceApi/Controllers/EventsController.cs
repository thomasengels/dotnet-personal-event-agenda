using Microsoft.AspNetCore.Mvc;

namespace EventServiceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetEvents()
    {
        return Ok(new string[] { "Event1", "Event2" });
    }

    [HttpGet("{id}")]
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
