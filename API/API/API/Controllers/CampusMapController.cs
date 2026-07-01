using API.Data;
using API.DTOs;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/campus-map")]
[Authorize]
[RequireOperationalTask("reception")]
public class CampusMapController : ControllerBase
{
    private const decimal DefaultW = 220m;
    private const decimal DefaultH = 120m;
    private const decimal DefaultGap = 24m;
    private const decimal DefaultStartX = 24m;
    private const decimal DefaultStartY = 24m;
    private const int DefaultColumns = 4;

    private readonly ApplicationDbContext _context;
    private readonly IAttendancePermissionService _permissionService;
    private readonly ICampusMapRealtimeService _realtimeService;
    private readonly ILogger<CampusMapController> _logger;

    public CampusMapController(
        ApplicationDbContext context,
        IAttendancePermissionService permissionService,
        ICampusMapRealtimeService realtimeService,
        ILogger<CampusMapController> logger)
    {
        _context = context;
        _permissionService = permissionService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    [HttpGet("layout")]
    public async Task<IActionResult> GetLayout(CancellationToken cancellationToken)
    {
        var gates = await _context.Gates
            .AsNoTracking()
            .OrderBy(g => g.GateId)
            .Select(g => new { g.GateId, g.GateName, g.Location })
            .ToListAsync(cancellationToken);

        if (gates.Count == 0)
        {
            return Ok(new
            {
                items = Array.Empty<object>(),
                message = "Chua co Gate nao de hien thi tren ban do."
            });
        }

        var layouts = await _context.CampusMapLayouts
            .AsNoTracking()
            .ToDictionaryAsync(l => l.GateId, cancellationToken);

        var realtime = await _realtimeService.BuildSnapshotAsync(DateTime.Now, cancellationToken);
        var realtimeByGate = realtime.Gates.ToDictionary(g => g.GateId);

        var items = gates
            .Select((gate, index) =>
            {
                var layout = layouts.TryGetValue(gate.GateId, out var persisted)
                    ? persisted
                    : BuildDefaultLayoutEntity(gate.GateId, index);

                var stats = realtimeByGate.TryGetValue(gate.GateId, out var stat)
                    ? stat
                    : new CampusGateRealtimeItem
                    {
                        GateId = gate.GateId,
                        GateName = gate.GateName,
                        Location = gate.Location,
                        Status = "Normal"
                    };

                return new
                {
                    gateId = gate.GateId,
                    gateName = gate.GateName,
                    location = gate.Location,
                    layout = new
                    {
                        x = layout.X,
                        y = layout.Y,
                        w = layout.W,
                        h = layout.H,
                        zIndex = layout.ZIndex,
                        color = layout.Color,
                        icon = layout.Icon,
                        isVisible = layout.IsVisible,
                        isLocked = layout.IsLocked
                    },
                    stats = new
                    {
                        cameraCount = stats.CameraCount,
                        onlineCameraCount = stats.OnlineCameraCount,
                        offlineCameraCount = stats.OfflineCameraCount,
                        lastAccessAt = stats.LastAccessAt,
                        recentAccessCount = stats.RecentAccessCount
                    },
                    status = stats.Status
                };
            })
            .ToList();

        return Ok(new
        {
            items,
            limitations = new[]
            {
                "Camera offline currently inferred from missing StreamUrl/UrlView due to schema not storing live health."
            }
        });
    }

    [HttpPut("layout")]
    public async Task<IActionResult> SaveLayoutBatch(
        [FromBody] CampusMapLayoutBatchUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await CanEditAsync()) return Forbid();

        var normalizedItems = request.Items
            .Select(item => new CampusMapLayoutUpsertItemRequest
            {
                GateId = item.GateId,
                X = item.X,
                Y = item.Y,
                W = item.W,
                H = item.H,
                ZIndex = item.ZIndex,
                Color = item.Color?.Trim(),
                Icon = item.Icon?.Trim(),
                IsVisible = item.IsVisible,
                IsLocked = item.IsLocked
            })
            .ToList();

        if (normalizedItems.Select(i => i.GateId).Distinct().Count() != normalizedItems.Count)
            return BadRequest(new { message = "Payload contains duplicate gateId." });

        var validationError = ValidateUpsertItems(normalizedItems);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        var gateIds = normalizedItems.Select(i => i.GateId).Distinct().ToList();
        var existingGateIds = await _context.Gates
            .AsNoTracking()
            .Where(g => gateIds.Contains(g.GateId))
            .Select(g => g.GateId)
            .ToListAsync(cancellationToken);

        var missingGateIds = gateIds.Except(existingGateIds).OrderBy(x => x).ToList();
        if (missingGateIds.Count > 0)
            return NotFound(new { message = "Mot so gateId khong ton tai.", gateIds = missingGateIds });

        var existingLayouts = await _context.CampusMapLayouts
            .Where(l => gateIds.Contains(l.GateId))
            .ToDictionaryAsync(l => l.GateId, cancellationToken);

        var currentUserId = GetCurrentUserId();
        var nowUtc = DateTime.UtcNow;

        foreach (var item in normalizedItems)
        {
            if (existingLayouts.TryGetValue(item.GateId, out var layout))
            {
                layout.X = item.X!.Value;
                layout.Y = item.Y!.Value;
                layout.W = item.W!.Value;
                layout.H = item.H!.Value;
                layout.ZIndex = item.ZIndex ?? layout.ZIndex;
                layout.Color = string.IsNullOrWhiteSpace(item.Color) ? null : item.Color;
                layout.Icon = string.IsNullOrWhiteSpace(item.Icon) ? null : item.Icon;
                layout.IsVisible = item.IsVisible ?? layout.IsVisible;
                layout.IsLocked = item.IsLocked ?? layout.IsLocked;
                layout.UpdatedAt = nowUtc;
                layout.UpdatedBy = currentUserId;
            }
            else
            {
                _context.CampusMapLayouts.Add(new CampusMapLayout
                {
                    GateId = item.GateId,
                    X = item.X!.Value,
                    Y = item.Y!.Value,
                    W = item.W!.Value,
                    H = item.H!.Value,
                    ZIndex = item.ZIndex ?? 1,
                    Color = string.IsNullOrWhiteSpace(item.Color) ? null : item.Color,
                    Icon = string.IsNullOrWhiteSpace(item.Icon) ? null : item.Icon,
                    IsVisible = item.IsVisible ?? true,
                    IsLocked = item.IsLocked ?? false,
                    UpdatedAt = nowUtc,
                    UpdatedBy = currentUserId
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Da luu campus map layout.", updated = normalizedItems.Count });
    }

    [HttpPatch("layout/{gateId:int}")]
    public async Task<IActionResult> UpdateSingleLayout(
        int gateId,
        [FromBody] CampusMapLayoutPatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!await CanEditAsync()) return Forbid();

        var gateExists = await _context.Gates.AsNoTracking().AnyAsync(g => g.GateId == gateId, cancellationToken);
        if (!gateExists)
            return NotFound(new { message = $"Khong tim thay gateId {gateId}." });

        if (request.W.HasValue && request.W.Value <= 0)
            return BadRequest(new { message = "W must be greater than 0." });
        if (request.H.HasValue && request.H.Value <= 0)
            return BadRequest(new { message = "H must be greater than 0." });

        var layout = await _context.CampusMapLayouts
            .FirstOrDefaultAsync(l => l.GateId == gateId, cancellationToken);

        if (layout == null)
        {
            var gateIndex = await _context.Gates
                .AsNoTracking()
                .CountAsync(g => g.GateId < gateId, cancellationToken);
            layout = BuildDefaultLayoutEntity(gateId, gateIndex);
            _context.CampusMapLayouts.Add(layout);
        }

        layout.X = request.X ?? layout.X;
        layout.Y = request.Y ?? layout.Y;
        layout.W = request.W ?? layout.W;
        layout.H = request.H ?? layout.H;
        layout.ZIndex = request.ZIndex ?? layout.ZIndex;
        layout.Color = request.Color?.Trim() ?? layout.Color;
        layout.Icon = request.Icon?.Trim() ?? layout.Icon;
        layout.IsVisible = request.IsVisible ?? layout.IsVisible;
        layout.IsLocked = request.IsLocked ?? layout.IsLocked;
        layout.UpdatedAt = DateTime.UtcNow;
        layout.UpdatedBy = GetCurrentUserId();

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            gateId = layout.GateId,
            x = layout.X,
            y = layout.Y,
            w = layout.W,
            h = layout.H,
            zIndex = layout.ZIndex,
            color = layout.Color,
            icon = layout.Icon,
            isVisible = layout.IsVisible,
            isLocked = layout.IsLocked,
            updatedAt = layout.UpdatedAt,
            updatedBy = layout.UpdatedBy
        });
    }

    [HttpGet("scene3d")]
    public async Task<IActionResult> GetScene3D(CancellationToken cancellationToken)
    {
        var objects = await _context.Campus3DObjects
            .AsNoTracking()
            .Include(o => o.Site)
            .Where(o => o.IsActive)
            .OrderBy(o => o.SiteId)
            .ThenBy(o => o.ObjectType)
            .ToListAsync(cancellationToken);

        var gateIds = objects.Where(o => o.ObjectType == "GateMarker").Select(o => o.SiteId).ToList();
        var realtime = await _realtimeService.BuildSnapshotAsync(DateTime.Now, cancellationToken);

        var sites = await _context.Sites
            .AsNoTracking()
            .Select(s => new { s.SiteId, s.Name, s.Code })
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            sites = sites.Select(s => new
            {
                siteId = s.SiteId,
                name = s.Name,
                code = s.Code,
                objects = objects.Where(o => o.SiteId == s.SiteId).Select(o => new
                {
                    id = o.Id,
                    type = o.ObjectType,
                    label = o.Label,
                    posX = o.PositionX,
                    posZ = o.PositionZ,
                    posY = o.PositionY,
                    width = o.Width,
                    length = o.Length,
                    height = o.Height,
                    floors = o.Floors,
                    rotation = o.Rotation,
                    color = o.Color,
                    properties = o.PropertiesJson,
                    isActive = o.IsActive
                })
            }),
            gates = realtime.Gates.Select(g => new
            {
                gateId = g.GateId,
                gateName = g.GateName,
                status = g.Status,
                cameraCount = g.CameraCount,
                offlineCameraCount = g.OfflineCameraCount,
                lastAccessAt = g.LastAccessAt,
                recentAccessCount = g.RecentAccessCount
            }),
            summary = new
            {
                siteCount = sites.Count,
                objectCount = objects.Count,
                onlineGates = realtime.Summary.ActiveGateCount,
                warningGates = realtime.Summary.WarningGateCount,
                offlineCameras = realtime.Summary.OfflineCameraCount
            },
            updatedAt = realtime.UpdatedAt
        });
    }

    [HttpPost("scene3d/objects")]
    public async Task<IActionResult> CreateSceneObject(
        [FromBody] Campus3DObjectUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync()) return Forbid();

        var validation = await ValidateSceneObjectRequestAsync(request, cancellationToken);
        if (validation != null) return validation;

        var entity = new Campus3DObject
        {
            SiteId = request.SiteId,
            ObjectType = request.ObjectType.Trim(),
            Label = request.Label.Trim(),
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            PositionZ = request.PositionZ,
            Width = request.Width,
            Length = request.Length,
            Height = request.Height,
            Floors = request.Floors,
            Rotation = request.Rotation,
            Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim(),
            PropertiesJson = string.IsNullOrWhiteSpace(request.PropertiesJson) ? null : request.PropertiesJson.Trim(),
            IsActive = request.IsActive ?? true,
        };

        _context.Campus3DObjects.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(BuildSceneObjectPayload(entity));
    }

    [HttpPatch("scene3d/objects/{objectId:int}")]
    public async Task<IActionResult> UpdateSceneObject(
        int objectId,
        [FromBody] Campus3DObjectPatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanEditAsync()) return Forbid();

        var entity = await _context.Campus3DObjects.FirstOrDefaultAsync(o => o.Id == objectId, cancellationToken);
        if (entity == null)
            return NotFound(new { message = "3D object not found." });

        if (request.SiteId.HasValue)
        {
            var siteExists = await _context.Sites.AsNoTracking().AnyAsync(s => s.SiteId == request.SiteId.Value, cancellationToken);
            if (!siteExists)
                return BadRequest(new { message = "Site does not exist." });
            entity.SiteId = request.SiteId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.ObjectType))
            entity.ObjectType = request.ObjectType.Trim();
        if (!string.IsNullOrWhiteSpace(request.Label))
            entity.Label = request.Label.Trim();

        entity.PositionX = request.PositionX ?? entity.PositionX;
        entity.PositionY = request.PositionY ?? entity.PositionY;
        entity.PositionZ = request.PositionZ ?? entity.PositionZ;
        entity.Width = request.Width ?? entity.Width;
        entity.Length = request.Length ?? entity.Length;
        entity.Height = request.Height ?? entity.Height;
        entity.Floors = request.Floors ?? entity.Floors;
        entity.Rotation = request.Rotation ?? entity.Rotation;
        entity.Color = request.Color != null ? request.Color.Trim() : entity.Color;
        entity.PropertiesJson = request.PropertiesJson != null ? request.PropertiesJson.Trim() : entity.PropertiesJson;
        entity.IsActive = request.IsActive ?? entity.IsActive;

        var numericError = ValidateSceneObjectNumbers(entity.Width, entity.Length, entity.Height);
        if (numericError != null)
            return BadRequest(new { message = numericError });

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(BuildSceneObjectPayload(entity));
    }

    [HttpDelete("scene3d/objects/{objectId:int}")]
    public async Task<IActionResult> DeleteSceneObject(int objectId, CancellationToken cancellationToken)
    {
        if (!await CanEditAsync()) return Forbid();

        var entity = await _context.Campus3DObjects.FirstOrDefaultAsync(o => o.Id == objectId, cancellationToken);
        if (entity == null)
            return NotFound(new { message = "3D object not found." });

        _context.Campus3DObjects.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("realtime")]
    public async Task<IActionResult> GetRealtime(CancellationToken cancellationToken)
    {
        var snapshot = await _realtimeService.BuildSnapshotAsync(DateTime.Now, cancellationToken);

        return Ok(new
        {
            updatedAt = snapshot.UpdatedAt,
            summary = new
            {
                activeGateCount = snapshot.Summary.ActiveGateCount,
                warningGateCount = snapshot.Summary.WarningGateCount,
                offlineCameraCount = snapshot.Summary.OfflineCameraCount,
                recentEventCount = snapshot.Summary.RecentEventCount
            },
            gates = snapshot.Gates.Select(g => new
            {
                gateId = g.GateId,
                status = g.Status,
                lastAccessAt = g.LastAccessAt,
                recentAccessCount = g.RecentAccessCount,
                cameraCount = g.CameraCount,
                offlineCameraCount = g.OfflineCameraCount,
                message = g.Message
            }),
            recentEvents = snapshot.RecentEvents
        });
    }

    private static string? ValidateUpsertItems(List<CampusMapLayoutUpsertItemRequest> items)
    {
        foreach (var item in items)
        {
            if (item.W.GetValueOrDefault() <= 0) return $"GateId {item.GateId}: W must be greater than 0.";
            if (item.H.GetValueOrDefault() <= 0) return $"GateId {item.GateId}: H must be greater than 0.";
        }

        return null;
    }

    private async Task<IActionResult?> ValidateSceneObjectRequestAsync(
        Campus3DObjectUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ObjectType))
            return BadRequest(new { message = "ObjectType is required." });
        if (string.IsNullOrWhiteSpace(request.Label))
            return BadRequest(new { message = "Label is required." });

        var siteExists = await _context.Sites.AsNoTracking().AnyAsync(s => s.SiteId == request.SiteId, cancellationToken);
        if (!siteExists)
            return BadRequest(new { message = "Site does not exist." });

        var numericError = ValidateSceneObjectNumbers(request.Width, request.Length, request.Height);
        if (numericError != null)
            return BadRequest(new { message = numericError });

        return null;
    }

    private static string? ValidateSceneObjectNumbers(decimal width, decimal length, decimal height)
    {
        if (width <= 0) return "Width must be greater than 0.";
        if (length <= 0) return "Length must be greater than 0.";
        if (height <= 0) return "Height must be greater than 0.";
        return null;
    }

    private static object BuildSceneObjectPayload(Campus3DObject entity)
    {
        return new
        {
            id = entity.Id,
            siteId = entity.SiteId,
            type = entity.ObjectType,
            label = entity.Label,
            posX = entity.PositionX,
            posY = entity.PositionY,
            posZ = entity.PositionZ,
            width = entity.Width,
            length = entity.Length,
            height = entity.Height,
            floors = entity.Floors,
            rotation = entity.Rotation,
            color = entity.Color,
            properties = entity.PropertiesJson,
            isActive = entity.IsActive
        };
    }

    private static CampusMapLayout BuildDefaultLayoutEntity(int gateId, int index)
    {
        var col = index % DefaultColumns;
        var row = index / DefaultColumns;

        return new CampusMapLayout
        {
            GateId = gateId,
            X = DefaultStartX + col * (DefaultW + DefaultGap),
            Y = DefaultStartY + row * (DefaultH + DefaultGap),
            W = DefaultW,
            H = DefaultH,
            ZIndex = 1,
            Color = "#0f766e",
            Icon = "gate",
            IsVisible = true,
            IsLocked = false,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task<bool> CanEditAsync() =>
        _permissionService.IsAdmin(User) || await _permissionService.IsManagerAsync(User);

    private int? GetCurrentUserId()
    {
        var claimCandidates = new[]
        {
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("nameid"),
            User.FindFirstValue("sub"),
            User.FindFirstValue("userId"),
            User.FindFirstValue("UserId"),
            User.FindFirstValue("id")
        };

        foreach (var rawValue in claimCandidates)
        {
            if (int.TryParse(rawValue, out var userId))
                return userId;
        }

        _logger.LogDebug("CampusMapController cannot parse current user id from claims.");
        return null;
    }

    public sealed record Campus3DObjectUpsertRequest(
        int SiteId,
        string ObjectType,
        string Label,
        decimal PositionX,
        decimal PositionY,
        decimal PositionZ,
        decimal Width,
        decimal Length,
        decimal Height,
        int? Floors,
        decimal Rotation,
        string? Color,
        string? PropertiesJson,
        bool? IsActive);

    public sealed record Campus3DObjectPatchRequest(
        int? SiteId,
        string? ObjectType,
        string? Label,
        decimal? PositionX,
        decimal? PositionY,
        decimal? PositionZ,
        decimal? Width,
        decimal? Length,
        decimal? Height,
        int? Floors,
        decimal? Rotation,
        string? Color,
        string? PropertiesJson,
        bool? IsActive);
}
