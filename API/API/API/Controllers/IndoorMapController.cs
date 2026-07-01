using API.Data;
using API.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/indoor-map")]
[Authorize]
[RequireOperationalTask("reception")]
public class IndoorMapController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public IndoorMapController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("nodes")]
    public async Task<IActionResult> GetNodes([FromQuery] int buildingId, [FromQuery] int? floorId)
    {
        var query = _db.IndoorPathNodes
            .Include(n => n.FacilityFloor)
            .Where(n => n.BuildingId == buildingId);

        if (floorId.HasValue)
            query = query.Where(n => n.FacilityFloorId == floorId);

        var nodes = await query.OrderBy(n => n.Id).Select(n => new
        {
            n.Id,
            n.BuildingId,
            n.FacilityFloorId,
            FacilityFloorName = n.FacilityFloor != null ? n.FacilityFloor.Name : null,
            n.Label,
            n.NodeType,
            n.X,
            n.Y,
            n.Z,
            n.IsEmergencyExit,
            n.IsAccessible,
            n.NeighborsJson
        }).ToListAsync();

        return Ok(new { data = nodes });
    }
}
