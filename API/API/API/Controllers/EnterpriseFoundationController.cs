using System.Security.Claims;
using API.Data;
using API.Middleware;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/foundation")]
[Authorize(Roles = "Admin,QuanLy")]
public class EnterpriseFoundationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyHierarchyBackfillService _backfillService;

    public EnterpriseFoundationController(
        ApplicationDbContext context,
        ICompanyHierarchyBackfillService backfillService)
    {
        _context = context;
        _backfillService = backfillService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var payload = new
        {
            Companies = await _context.Companies.CountAsync(),
            Sites = await _context.Sites.CountAsync(),
            Buildings = await _context.Buildings.CountAsync(),
            Floors = await _context.FacilityFloors.CountAsync(),
            Zones = await _context.SecurityZones.CountAsync(),
            AccessPoints = await _context.AccessPoints.CountAsync(),
            Doors = await _context.Doors.CountAsync(),
            Lanes = await _context.Lanes.CountAsync(),
            MusterPoints = await _context.MusterPoints.CountAsync(),
            IdentityProviders = await _context.ExternalIdentityProviders.CountAsync(),
            IdentityMappings = await _context.ExternalIdentityMappings.CountAsync(),
            RecertificationCampaigns = await _context.AccessRecertificationCampaigns.CountAsync()
        };

        return Ok(payload);
    }

    [HttpGet("hierarchy")]
    public async Task<IActionResult> GetHierarchy()
    {
        var companies = await _context.Companies
            .Include(c => c.Sites)
                .ThenInclude(s => s.Buildings)
                    .ThenInclude(b => b.Floors)
            .Include(c => c.Sites)
                .ThenInclude(s => s.Zones)
                    .ThenInclude(z => z.AccessPoints)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(companies.Select(company => new
        {
            company.CompanyId,
            company.Name,
            company.Code,
            company.IsActive,
            Sites = company.Sites.OrderBy(site => site.Name).Select(site => new
            {
                site.SiteId,
                site.Name,
                site.Code,
                site.Address,
                site.TimeZoneId,
                site.IsActive,
                Buildings = site.Buildings.OrderBy(building => building.Name).Select(building => new
                {
                    building.BuildingId,
                    building.Name,
                    building.Code,
                    building.IsActive,
                    Floors = building.Floors.OrderBy(floor => floor.SortOrder).Select(floor => new
                    {
                        floor.FacilityFloorId,
                        floor.Name,
                        floor.Code,
                        floor.SortOrder,
                        floor.IsActive
                    })
                }),
                Zones = site.Zones.OrderBy(zone => zone.Name).Select(zone => new
                {
                    zone.SecurityZoneId,
                    zone.SiteId,
                    zone.BuildingId,
                    zone.FacilityFloorId,
                    zone.Name,
                    zone.Code,
                    zone.SecurityLevel,
                    zone.IsRestricted,
                    zone.IsActive,
                    AccessPoints = zone.AccessPoints.OrderBy(accessPoint => accessPoint.Name).Select(accessPoint => new
                    {
                        accessPoint.AccessPointId,
                        accessPoint.Name,
                        accessPoint.Type,
                        accessPoint.DirectionMode,
                        accessPoint.IsActive
                    })
                })
            })
        }));
    }

    [HttpGet("hierarchy/search")]
    public async Task<IActionResult> SearchHierarchy([FromQuery] string? type, [FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<object>());

        var term = q.Trim().ToUpperInvariant();
        var results = new List<object>();

        switch (type.ToLowerInvariant())
        {
            case "company":
                results.AddRange(await _context.Companies
                    .Where(x => x.Name.ToUpper().Contains(term) || x.Code.ToUpper().Contains(term))
                    .Select(x => new { x.CompanyId, x.Name, x.Code, EntityType = "Company", ParentId = (int?)null, ParentName = (string?)null })
                    .ToListAsync());
                break;
            case "site":
                results.AddRange(await _context.Sites
                    .Include(x => x.Company)
                    .Where(x => x.Name.ToUpper().Contains(term) || x.Code.ToUpper().Contains(term))
                    .Select(x => new { SiteId = x.SiteId, x.Name, x.Code, EntityType = "Site", ParentId = (int?)x.CompanyId, ParentName = x.Company!.Name })
                    .ToListAsync());
                break;
            case "building":
                results.AddRange(await _context.Buildings
                    .Include(x => x.Site)
                    .Where(x => x.Name.ToUpper().Contains(term) || x.Code.ToUpper().Contains(term))
                    .Select(x => new { BuildingId = x.BuildingId, x.Name, x.Code, EntityType = "Building", ParentId = (int?)x.SiteId, ParentName = x.Site!.Name })
                    .ToListAsync());
                break;
            case "zone":
                results.AddRange(await _context.SecurityZones
                    .Include(x => x.Site)
                    .Where(x => x.Name.ToUpper().Contains(term) || x.Code.ToUpper().Contains(term))
                    .Select(x => new { x.SecurityZoneId, x.Name, x.Code, EntityType = "Zone", ParentId = (int?)x.SiteId, ParentName = x.Site!.Name })
                    .ToListAsync());
                break;
            default:
                results.AddRange(await _context.AccessPoints
                    .Include(x => x.Site)
                    .Where(x => x.Name.ToUpper().Contains(term) || (type == "accesspoint" || type == "all"))
                    .Take(50)
                    .Select(x => new { x.AccessPointId, x.Name, Type = x.Type, EntityType = "AccessPoint", ParentId = (int?)x.SiteId, ParentName = x.Site!.Name })
                    .ToListAsync());
                break;
        }

        return Ok(results.Take(50));
    }

    [HttpGet("backfill/status")]
    public async Task<IActionResult> GetBackfillStatus()
    {
        var assetMap = await _backfillService.GetAssetMapAsync();
        return Ok(new
        {
            GatesMapped = assetMap.Gates.Count(g => g.SiteId.HasValue),
            GatesUnmapped = assetMap.Gates.Count(g => !g.SiteId.HasValue),
            CamerasMapped = assetMap.Cameras.Count(c => c.SiteId.HasValue),
            CamerasUnmapped = assetMap.Cameras.Count(c => !c.SiteId.HasValue),
            VehiclesMapped = assetMap.Vehicles.Count(v => v.SiteId.HasValue),
            VehiclesUnmapped = assetMap.Vehicles.Count(v => !v.SiteId.HasValue),
            TotalGates = assetMap.Gates.Count,
            TotalCameras = assetMap.Cameras.Count,
            TotalVehicles = assetMap.Vehicles.Count
        });
    }

    [HttpGet("asset-map")]
    public async Task<IActionResult> GetAssetMap(CancellationToken cancellationToken)
    {
        var report = await _backfillService.GetAssetMapAsync(cancellationToken);
        return Ok(report);
    }

    [HttpPost("backfill/default-site")]
    [RequireStepUp(PrivilegedActions.SiteHierarchyBackfill)]
    public async Task<IActionResult> BackfillDefaultSite(
        [FromBody] CompanyHierarchyBackfillRequest? request,
        CancellationToken cancellationToken)
    {
        var report = await _backfillService.BackfillDefaultSiteAsync(
            request ?? new CompanyHierarchyBackfillRequest(null, null, null, null, null),
            GetCurrentUserId(),
            cancellationToken);
        return Ok(report);
    }

    [HttpPost("companies")]
    public async Task<IActionResult> CreateCompany([FromBody] CompanyRequest request)
    {
        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        var company = new Company
        {
            Name = request.Name.Trim(),
            Code = request.Code.Trim()
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetHierarchy), new { id = company.CompanyId }, company);
    }

    [HttpPatch("companies/{companyId:int}")]
    public async Task<IActionResult> UpdateCompany(int companyId, [FromBody] CompanyPatchRequest request)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
        if (company == null)
            return NotFound(new { message = "Company not found." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        company.Name = request.Name.Trim();
        company.Code = request.Code.Trim();
        company.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(company);
    }

    [HttpDelete("companies/{companyId:int}")]
    public async Task<IActionResult> DeleteCompany(int companyId)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
        if (company == null)
            return NotFound(new { message = "Company not found." });

        company.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(company);
    }

    [HttpPatch("companies/{companyId:int}/restore")]
    public async Task<IActionResult> RestoreCompany(int companyId)
    {
        var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
        if (company == null)
            return NotFound(new { message = "Company not found." });

        company.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(company);
    }

    [HttpPost("sites")]
    public async Task<IActionResult> CreateSite([FromBody] SiteRequest request)
    {
        if (!await _context.Companies.AnyAsync(c => c.CompanyId == request.CompanyId))
            return BadRequest(new { message = "Company does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        var site = new Site
        {
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            Address = request.Address?.Trim(),
            TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId) ? "Asia/Ho_Chi_Minh" : request.TimeZoneId.Trim()
        };

        _context.Sites.Add(site);
        await _context.SaveChangesAsync();
        return Ok(site);
    }

    [HttpPatch("sites/{siteId:int}")]
    public async Task<IActionResult> UpdateSite(int siteId, [FromBody] SitePatchRequest request)
    {
        var site = await _context.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId);
        if (site == null)
            return NotFound(new { message = "Site not found." });

        if (request.CompanyId.HasValue && !await _context.Companies.AnyAsync(c => c.CompanyId == request.CompanyId.Value))
            return BadRequest(new { message = "Company does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        site.CompanyId = request.CompanyId ?? site.CompanyId;
        site.Name = request.Name.Trim();
        site.Code = request.Code.Trim();
        site.Address = request.Address?.Trim();
        site.TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId) ? site.TimeZoneId : request.TimeZoneId.Trim();
        site.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(site);
    }

    [HttpDelete("sites/{siteId:int}")]
    public async Task<IActionResult> DeleteSite(int siteId)
    {
        var site = await _context.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId);
        if (site == null)
            return NotFound(new { message = "Site not found." });

        site.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(site);
    }

    [HttpPatch("sites/{siteId:int}/restore")]
    public async Task<IActionResult> RestoreSite(int siteId)
    {
        var site = await _context.Sites.FirstOrDefaultAsync(s => s.SiteId == siteId);
        if (site == null)
            return NotFound(new { message = "Site not found." });

        site.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(site);
    }

    [HttpPost("buildings")]
    public async Task<IActionResult> CreateBuilding([FromBody] BuildingRequest request)
    {
        if (!await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId))
            return BadRequest(new { message = "Site does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        var building = new Building
        {
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim()
        };

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();
        return Ok(building);
    }

    [HttpPatch("buildings/{buildingId:int}")]
    public async Task<IActionResult> UpdateBuilding(int buildingId, [FromBody] BuildingPatchRequest request)
    {
        var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
        if (building == null)
            return NotFound(new { message = "Building not found." });

        if (request.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId.Value))
            return BadRequest(new { message = "Site does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        building.SiteId = request.SiteId ?? building.SiteId;
        building.Name = request.Name.Trim();
        building.Code = request.Code.Trim();
        building.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(building);
    }

    [HttpDelete("buildings/{buildingId:int}")]
    public async Task<IActionResult> DeleteBuilding(int buildingId)
    {
        var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
        if (building == null)
            return NotFound(new { message = "Building not found." });

        building.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(building);
    }

    [HttpPatch("buildings/{buildingId:int}/restore")]
    public async Task<IActionResult> RestoreBuilding(int buildingId)
    {
        var building = await _context.Buildings.FirstOrDefaultAsync(b => b.BuildingId == buildingId);
        if (building == null)
            return NotFound(new { message = "Building not found." });

        building.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(building);
    }

    [HttpPost("floors")]
    public async Task<IActionResult> CreateFloor([FromBody] FloorRequest request)
    {
        if (!await _context.Buildings.AnyAsync(b => b.BuildingId == request.BuildingId))
            return BadRequest(new { message = "Building does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        var floor = new FacilityFloor
        {
            BuildingId = request.BuildingId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            SortOrder = request.SortOrder
        };

        _context.FacilityFloors.Add(floor);
        await _context.SaveChangesAsync();
        return Ok(floor);
    }

    [HttpPatch("floors/{floorId:int}")]
    public async Task<IActionResult> UpdateFloor(int floorId, [FromBody] FloorPatchRequest request)
    {
        var floor = await _context.FacilityFloors.FirstOrDefaultAsync(f => f.FacilityFloorId == floorId);
        if (floor == null)
            return NotFound(new { message = "Floor not found." });

        if (request.BuildingId.HasValue && !await _context.Buildings.AnyAsync(b => b.BuildingId == request.BuildingId.Value))
            return BadRequest(new { message = "Building does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        floor.BuildingId = request.BuildingId ?? floor.BuildingId;
        floor.Name = request.Name.Trim();
        floor.Code = request.Code.Trim();
        floor.SortOrder = request.SortOrder;
        floor.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(floor);
    }

    [HttpDelete("floors/{floorId:int}")]
    public async Task<IActionResult> DeleteFloor(int floorId)
    {
        var floor = await _context.FacilityFloors.FirstOrDefaultAsync(f => f.FacilityFloorId == floorId);
        if (floor == null)
            return NotFound(new { message = "Floor not found." });

        floor.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(floor);
    }

    [HttpPatch("floors/{floorId:int}/restore")]
    public async Task<IActionResult> RestoreFloor(int floorId)
    {
        var floor = await _context.FacilityFloors.FirstOrDefaultAsync(f => f.FacilityFloorId == floorId);
        if (floor == null)
            return NotFound(new { message = "Floor not found." });

        floor.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(floor);
    }

    [HttpPost("zones")]
    public async Task<IActionResult> CreateZone([FromBody] ZoneRequest request)
    {
        if (!await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId))
            return BadRequest(new { message = "Site does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        var zone = new SecurityZone
        {
            SiteId = request.SiteId,
            BuildingId = request.BuildingId,
            FacilityFloorId = request.FacilityFloorId,
            Name = request.Name.Trim(),
            Code = request.Code.Trim(),
            SecurityLevel = string.IsNullOrWhiteSpace(request.SecurityLevel) ? "Normal" : request.SecurityLevel.Trim(),
            IsRestricted = request.IsRestricted
        };

        _context.SecurityZones.Add(zone);
        await _context.SaveChangesAsync();
        return Ok(zone);
    }

    [HttpPatch("zones/{zoneId:int}")]
    public async Task<IActionResult> UpdateZone(int zoneId, [FromBody] ZonePatchRequest request)
    {
        var zone = await _context.SecurityZones.FirstOrDefaultAsync(z => z.SecurityZoneId == zoneId);
        if (zone == null)
            return NotFound(new { message = "Zone not found." });

        if (request.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId.Value))
            return BadRequest(new { message = "Site does not exist." });
        if (request.BuildingId.HasValue && !await _context.Buildings.AnyAsync(b => b.BuildingId == request.BuildingId.Value))
            return BadRequest(new { message = "Building does not exist." });
        if (request.FacilityFloorId.HasValue && !await _context.FacilityFloors.AnyAsync(f => f.FacilityFloorId == request.FacilityFloorId.Value))
            return BadRequest(new { message = "Floor does not exist." });

        var validation = ValidateNameCode(request.Name, request.Code);
        if (validation != null) return validation;

        zone.SiteId = request.SiteId ?? zone.SiteId;
        zone.BuildingId = request.BuildingId;
        zone.FacilityFloorId = request.FacilityFloorId;
        zone.Name = request.Name.Trim();
        zone.Code = request.Code.Trim();
        zone.SecurityLevel = string.IsNullOrWhiteSpace(request.SecurityLevel) ? "Normal" : request.SecurityLevel.Trim();
        zone.IsRestricted = request.IsRestricted;
        zone.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(zone);
    }

    [HttpDelete("zones/{zoneId:int}")]
    public async Task<IActionResult> DeleteZone(int zoneId)
    {
        var zone = await _context.SecurityZones.FirstOrDefaultAsync(z => z.SecurityZoneId == zoneId);
        if (zone == null)
            return NotFound(new { message = "Zone not found." });

        zone.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(zone);
    }

    [HttpPatch("zones/{zoneId:int}/restore")]
    public async Task<IActionResult> RestoreZone(int zoneId)
    {
        var zone = await _context.SecurityZones.FirstOrDefaultAsync(z => z.SecurityZoneId == zoneId);
        if (zone == null)
            return NotFound(new { message = "Zone not found." });

        zone.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(zone);
    }

    [HttpPost("access-points")]
    public async Task<IActionResult> CreateAccessPoint([FromBody] AccessPointRequest request)
    {
        if (!await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId))
            return BadRequest(new { message = "Site does not exist." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var accessPoint = new AccessPoint
        {
            SiteId = request.SiteId,
            SecurityZoneId = request.SecurityZoneId,
            Name = request.Name.Trim(),
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Door" : request.Type.Trim(),
            DirectionMode = string.IsNullOrWhiteSpace(request.DirectionMode) ? "Bidirectional" : request.DirectionMode.Trim()
        };

        _context.AccessPoints.Add(accessPoint);
        await _context.SaveChangesAsync();
        return Ok(accessPoint);
    }

    [HttpPatch("access-points/{accessPointId:int}")]
    public async Task<IActionResult> UpdateAccessPoint(int accessPointId, [FromBody] AccessPointPatchRequest request)
    {
        var accessPoint = await _context.AccessPoints.FirstOrDefaultAsync(a => a.AccessPointId == accessPointId);
        if (accessPoint == null)
            return NotFound(new { message = "Access point not found." });

        if (request.SiteId.HasValue && !await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId.Value))
            return BadRequest(new { message = "Site does not exist." });
        if (request.SecurityZoneId.HasValue && !await _context.SecurityZones.AnyAsync(z => z.SecurityZoneId == request.SecurityZoneId.Value))
            return BadRequest(new { message = "Security zone does not exist." });
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        accessPoint.SiteId = request.SiteId ?? accessPoint.SiteId;
        accessPoint.SecurityZoneId = request.SecurityZoneId;
        accessPoint.Name = request.Name.Trim();
        accessPoint.Type = string.IsNullOrWhiteSpace(request.Type) ? "Door" : request.Type.Trim();
        accessPoint.DirectionMode = string.IsNullOrWhiteSpace(request.DirectionMode) ? "Bidirectional" : request.DirectionMode.Trim();
        accessPoint.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return Ok(accessPoint);
    }

    [HttpDelete("access-points/{accessPointId:int}")]
    public async Task<IActionResult> DeleteAccessPoint(int accessPointId)
    {
        var accessPoint = await _context.AccessPoints.FirstOrDefaultAsync(a => a.AccessPointId == accessPointId);
        if (accessPoint == null)
            return NotFound(new { message = "Access point not found." });

        accessPoint.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok(accessPoint);
    }

    [HttpPatch("access-points/{accessPointId:int}/restore")]
    public async Task<IActionResult> RestoreAccessPoint(int accessPointId)
    {
        var accessPoint = await _context.AccessPoints.FirstOrDefaultAsync(a => a.AccessPointId == accessPointId);
        if (accessPoint == null)
            return NotFound(new { message = "Access point not found." });

        accessPoint.IsActive = true;
        await _context.SaveChangesAsync();
        return Ok(accessPoint);
    }

    [HttpPost("doors")]
    public async Task<IActionResult> CreateDoor([FromBody] DoorRequest request)
    {
        if (!await _context.AccessPoints.AnyAsync(a => a.AccessPointId == request.AccessPointId))
            return BadRequest(new { message = "Access point does not exist." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var door = new Door
        {
            AccessPointId = request.AccessPointId,
            Name = request.Name.Trim(),
            DoorMode = string.IsNullOrWhiteSpace(request.DoorMode) ? "Normal" : request.DoorMode.Trim()
        };

        _context.Doors.Add(door);
        await _context.SaveChangesAsync();
        return Ok(door);
    }

    [HttpPost("lanes")]
    public async Task<IActionResult> CreateLane([FromBody] LaneRequest request)
    {
        if (!await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId))
            return BadRequest(new { message = "Site does not exist." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var lane = new Lane
        {
            SiteId = request.SiteId,
            GateId = request.GateId,
            AccessPointId = request.AccessPointId,
            Name = request.Name.Trim(),
            Direction = string.IsNullOrWhiteSpace(request.Direction) ? "Entry" : request.Direction.Trim()
        };

        _context.Lanes.Add(lane);
        await _context.SaveChangesAsync();
        return Ok(lane);
    }

    [HttpPost("muster-points")]
    public async Task<IActionResult> CreateMusterPoint([FromBody] MusterPointRequest request)
    {
        if (!await _context.Sites.AnyAsync(s => s.SiteId == request.SiteId))
            return BadRequest(new { message = "Site does not exist." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var point = new MusterPoint
        {
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            LocationNote = request.LocationNote?.Trim(),
            Capacity = request.Capacity
        };

        _context.MusterPoints.Add(point);
        await _context.SaveChangesAsync();
        return Ok(point);
    }

    [HttpPost("identity-providers")]
    public async Task<IActionResult> CreateIdentityProvider([FromBody] IdentityProviderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Authority))
            return BadRequest(new { message = "Name and authority are required." });

        var provider = new ExternalIdentityProvider
        {
            Name = request.Name.Trim(),
            Protocol = string.IsNullOrWhiteSpace(request.Protocol) ? "OIDC" : request.Protocol.Trim(),
            Authority = request.Authority.Trim(),
            ClientId = request.ClientId?.Trim(),
            IsEnabled = request.IsEnabled
        };

        _context.ExternalIdentityProviders.Add(provider);
        await _context.SaveChangesAsync();
        return Ok(provider);
    }

    [HttpPost("identity-mappings")]
    public async Task<IActionResult> CreateIdentityMapping([FromBody] IdentityMappingRequest request)
    {
        if (!await _context.ExternalIdentityProviders.AnyAsync(p => p.ExternalIdentityProviderId == request.ProviderId))
            return BadRequest(new { message = "Identity provider does not exist." });

        if (string.IsNullOrWhiteSpace(request.ExternalSubject))
            return BadRequest(new { message = "ExternalSubject is required." });

        var mapping = new ExternalIdentityMapping
        {
            ExternalIdentityProviderId = request.ProviderId,
            UserId = request.UserId,
            EmployeeId = request.EmployeeId,
            ExternalSubject = request.ExternalSubject.Trim(),
            ExternalUsername = request.ExternalUsername?.Trim(),
            LastSyncedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        _context.ExternalIdentityMappings.Add(mapping);
        await _context.SaveChangesAsync();
        return Ok(mapping);
    }

    [HttpPatch("employees/{employeeId:int}/lifecycle")]
    public async Task<IActionResult> UpdateEmployeeLifecycle(int employeeId, [FromBody] EmployeeLifecycleRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null)
            return NotFound(new { message = "Employee not found." });

        if (string.IsNullOrWhiteSpace(request.NewState))
            return BadRequest(new { message = "NewState is required." });

        var previousState = employee.LifecycleStatus;
        var newState = request.NewState.Trim();
        employee.LifecycleStatus = newState;
        employee.PrimarySiteId = request.PrimarySiteId ?? employee.PrimarySiteId;
        employee.ManagerEmployeeId = request.ManagerEmployeeId ?? employee.ManagerEmployeeId;
        employee.LifecycleUpdatedAtUtc = DateTime.UtcNow;

        if (newState is EmployeeLifecycleStates.Terminated or EmployeeLifecycleStates.Suspended or EmployeeLifecycleStates.ContractorExpired)
        {
            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
            if (user != null)
            {
                user.IsActive = false;
                user.TokenVersion++;
                var tokens = await _context.UserRefreshTokens
                    .Where(token => token.UserId == user.UserId && token.RevokedAtUtc == null)
                    .ToListAsync();
                foreach (var token in tokens)
                {
                    token.RevokedAtUtc = DateTime.UtcNow;
                    token.RevocationReason = $"Lifecycle changed to {newState}";
                }
            }
        }

        _context.EmployeeLifecycleEvents.Add(new EmployeeLifecycleEvent
        {
            EmployeeId = employeeId,
            PreviousState = previousState,
            NewState = newState,
            Reason = request.Reason?.Trim(),
            ChangedByUserId = GetCurrentUserId()
        });

        await _context.SaveChangesAsync();
        return Ok(new { employee.EmployeeId, employee.LifecycleStatus, employee.PrimarySiteId, employee.ManagerEmployeeId });
    }

    [HttpPost("recertification-campaigns")]
    public async Task<IActionResult> CreateRecertificationCampaign([FromBody] RecertificationCampaignRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var campaign = new AccessRecertificationCampaign
        {
            Name = request.Name.Trim(),
            SiteId = request.SiteId,
            PeriodStartUtc = request.PeriodStartUtc,
            PeriodEndUtc = request.PeriodEndUtc,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Draft" : request.Status.Trim()
        };

        _context.AccessRecertificationCampaigns.Add(campaign);
        await _context.SaveChangesAsync();
        return Ok(campaign);
    }

    [HttpPost("recertification-campaigns/{campaignId:int}/decisions")]
    public async Task<IActionResult> RecordRecertificationDecision(int campaignId, [FromBody] RecertificationDecisionRequest request)
    {
        if (!await _context.AccessRecertificationCampaigns.AnyAsync(c => c.AccessRecertificationCampaignId == campaignId))
            return NotFound(new { message = "Campaign not found." });

        if (!await _context.Employees.AnyAsync(e => e.EmployeeId == request.EmployeeId))
            return BadRequest(new { message = "Employee not found." });

        var decision = new AccessRecertificationDecision
        {
            AccessRecertificationCampaignId = campaignId,
            EmployeeId = request.EmployeeId,
            ReviewerUserId = GetCurrentUserId(),
            Decision = string.IsNullOrWhiteSpace(request.Decision) ? "Pending" : request.Decision.Trim(),
            Notes = request.Notes?.Trim(),
            DecidedAtUtc = DateTime.UtcNow
        };

        _context.AccessRecertificationDecisions.Add(decision);
        await _context.SaveChangesAsync();
        return Ok(decision);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private BadRequestObjectResult? ValidateNameCode(string? name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Name is required." });
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Code is required." });
        return null;
    }

    public sealed record CompanyRequest(string Name, string Code);
    public sealed record CompanyPatchRequest(string Name, string Code, bool IsActive);
    public sealed record SiteRequest(int CompanyId, string Name, string Code, string? Address, string? TimeZoneId);
    public sealed record SitePatchRequest(int? CompanyId, string Name, string Code, string? Address, string? TimeZoneId, bool IsActive);
    public sealed record BuildingRequest(int SiteId, string Name, string Code);
    public sealed record BuildingPatchRequest(int? SiteId, string Name, string Code, bool IsActive);
    public sealed record FloorRequest(int BuildingId, string Name, string Code, int SortOrder);
    public sealed record FloorPatchRequest(int? BuildingId, string Name, string Code, int SortOrder, bool IsActive);
    public sealed record ZoneRequest(int SiteId, int? BuildingId, int? FacilityFloorId, string Name, string Code, string? SecurityLevel, bool IsRestricted);
    public sealed record ZonePatchRequest(int? SiteId, int? BuildingId, int? FacilityFloorId, string Name, string Code, string? SecurityLevel, bool IsRestricted, bool IsActive);
    public sealed record AccessPointRequest(int SiteId, int? SecurityZoneId, string Name, string? Type, string? DirectionMode);
    public sealed record AccessPointPatchRequest(int? SiteId, int? SecurityZoneId, string Name, string? Type, string? DirectionMode, bool IsActive);
    public sealed record DoorRequest(int AccessPointId, string Name, string? DoorMode);
    public sealed record LaneRequest(int SiteId, int? GateId, int? AccessPointId, string Name, string? Direction);
    public sealed record MusterPointRequest(int SiteId, string Name, string? LocationNote, int? Capacity);
    public sealed record IdentityProviderRequest(string Name, string? Protocol, string Authority, string? ClientId, bool IsEnabled);
    public sealed record IdentityMappingRequest(int ProviderId, int? UserId, int? EmployeeId, string ExternalSubject, string? ExternalUsername);
    public sealed record EmployeeLifecycleRequest(string NewState, string? Reason, int? PrimarySiteId, int? ManagerEmployeeId);
    public sealed record RecertificationCampaignRequest(string Name, int? SiteId, DateTime PeriodStartUtc, DateTime PeriodEndUtc, string? Status);
    public sealed record RecertificationDecisionRequest(int EmployeeId, string? Decision, string? Notes);
}
