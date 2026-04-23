using Microsoft.AspNetCore.Mvc;

namespace Worker.Presentation.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth() => Ok();
}
