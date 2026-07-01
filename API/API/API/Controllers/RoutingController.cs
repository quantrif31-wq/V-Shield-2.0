using API.Middleware;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireOperationalTask("monitoring")]
public class RoutingController : ControllerBase
{
    private readonly IRoutingService _routingService;

    public RoutingController(IRoutingService routingService)
    {
        _routingService = routingService;
    }

    [HttpPost]
    public async Task<IActionResult> GetRoute([FromBody] RouteRequest request)
    {
        var result = await _routingService.GetRouteAsync(request);
        return Ok(new { data = result });
    }
}
