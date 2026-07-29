using Microsoft.AspNetCore.Mvc;

namespace Bff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
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
}