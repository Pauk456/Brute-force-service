using Manager.Application.Services;
using Manager.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Presentation.Controllers;

[ApiController]
[Route("internal/api/manager/hash/crack/request")]
public sealed class ManagerInternalController : ControllerBase
{
    private readonly HashCrackService _service;

    public ManagerInternalController(HashCrackService service)
    {
        _service = service;
    }

    [HttpPatch]
    [Consumes("application/xml", "text/xml")]
    public IActionResult AcceptWorkerResult([FromBody] WorkerCrackResultResponse response)
    {
        var accepted = _service.TryApplyWorkerResult(response);
        if (!accepted)
        {
            return NotFound();
        }

        return Ok();
    }
}
