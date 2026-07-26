using Microsoft.AspNetCore.Mvc;

namespace Agenda.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgendaController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAgenda()
    {
        return Ok(new[] { "09:00 - Team standup", "13:00 - Client sync" });
    }
}