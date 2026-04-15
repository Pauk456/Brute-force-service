using Microsoft.AspNetCore.Mvc;
using Worker.Application.Services;
using Worker.Contracts;

namespace Worker.Presentation.Controllers;

[ApiController]
[Route("internal/api/worker/hash/crack/task")]
public sealed class WorkerTaskController : ControllerBase
{
    private readonly BruteForceCrackService _service;

    public WorkerTaskController(BruteForceCrackService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessTask([FromBody] WorkerCrackTaskRequest request, CancellationToken cancellationToken)
    {
        if (request.MaxLength <= 0 || request.PartCount <= 0 || request.PartNumber < 0 || request.PartNumber >= request.PartCount)
        {
            return BadRequest("Task partition values are invalid.");
        }

        await _service.ProcessTaskAsync(request, cancellationToken);
        return Ok();
    }
}
