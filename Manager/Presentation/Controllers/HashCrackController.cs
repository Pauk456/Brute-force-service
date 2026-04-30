using Manager.Application.Models;
using Manager.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Presentation.Controllers;

[ApiController]
[Route("api/hash")]
public sealed class HashCrackController : ControllerBase
{
    private readonly HashCrackService _service;

    public HashCrackController(HashCrackService service)
    {
        _service = service;
    }

    [HttpPost("crack")]
    public async Task<ActionResult<CrackResponseDto>> StartCrack([FromBody] CrackRequestDto request, CancellationToken cancellationToken)
    {
        if (request.MaxLength <= 0 || string.IsNullOrWhiteSpace(request.Hash))
        {
            return BadRequest("hash and maxLength are required.");
        }

        var response = await _service.StartCrackAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("status")]
    public ActionResult<CrackStatusResponseDto> GetStatus([FromQuery] Guid requestId)
    {
        var status = _service.GetStatus(requestId);
        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }
}
