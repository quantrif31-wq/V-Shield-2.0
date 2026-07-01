using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Route("api/runtime-services")]
[ApiController]
[Authorize]
[RequireOperationalTask("monitoring")]
public class RuntimeServicesController : ControllerBase
{
    private readonly RuntimeOrchestrator _runtimeOrchestrator;

    public RuntimeServicesController(RuntimeOrchestrator runtimeOrchestrator)
    {
        _runtimeOrchestrator = runtimeOrchestrator;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_runtimeOrchestrator.GetServices());
    }

    [HttpPut("{name}")]
    public IActionResult UpdateConfig(string name, [FromBody] UpdateRuntimeServiceRequest request)
    {
        var updated = _runtimeOrchestrator.UpdateConfig(name, request.Enabled, request.AutoStart);
        if (updated == null) return NotFound(new { message = $"Khong tim thay service {name}" });
        return Ok(updated);
    }

    [HttpPost("{name}/start")]
    public async Task<IActionResult> StartService(string name)
    {
        var result = await _runtimeOrchestrator.StartAsync(name);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpPost("{name}/stop")]
    public async Task<IActionResult> StopService(string name)
    {
        var result = await _runtimeOrchestrator.StopAsync(name);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }
}

public sealed class UpdateRuntimeServiceRequest
{
    public bool? Enabled { get; set; }
    public bool? AutoStart { get; set; }
}
