using API.Services.CameraRelay;
using API.Services.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace API.Controllers;

[ApiController]
[Route("api/camera-relay")]
[Authorize]
public sealed class CameraRelayController(CameraRelayRegistry registry, IOptions<SyncRuntimeOptions> options) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var mode = options.Value.Mode;
        return Ok(new
        {
            mode,
            enabled = string.Equals(mode, SyncRuntimeModes.Central, StringComparison.OrdinalIgnoreCase),
            nodes = registry.GetOnlineNodeIds()
        });
    }
}
