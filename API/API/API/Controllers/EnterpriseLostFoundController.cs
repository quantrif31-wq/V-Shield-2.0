using System.Security.Claims;
using API.Data;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/lost-found")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseLostFoundController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly LostFoundMatchingService _matchingService;
    private readonly LockerService _lockerService;
    private readonly EvidenceCaptureService _evidenceCapture;

    public EnterpriseLostFoundController(
        ApplicationDbContext context,
        LostFoundMatchingService matchingService,
        LockerService lockerService,
        EvidenceCaptureService evidenceCapture)
    {
        _context = context;
        _matchingService = matchingService;
        _lockerService = lockerService;
        _evidenceCapture = evidenceCapture;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            PendingLostItems = await _context.LostItemReports.CountAsync(l => l.Status == "Pending"),
            UnclaimedFoundItems = await _context.FoundItemReports.CountAsync(f => f.Status == "Unclaimed"),
            SuggestedMatches = await _context.ItemMatches.CountAsync(m => m.Status == "Suggested"),
            PendingClaims = await _context.ClaimRequests.CountAsync(c => c.Status == "Pending"),
            TotalCabinets = await _context.LockerCabinets.CountAsync(c => c.IsActive),
            AvailableCompartments = await _context.LockerCompartments.CountAsync(c => c.Status == "Empty"),
            OccupiedCompartments = await _context.LockerCompartments.CountAsync(c => c.Status == "Occupied")
        });
    }

    [HttpPost("lost-items")]
    public async Task<IActionResult> CreateLostItemReport([FromBody] LostItemReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ReporterName))
            return BadRequest(new { message = "ReporterName is required." });
        if (string.IsNullOrWhiteSpace(request.ReporterPhone))
            return BadRequest(new { message = "ReporterPhone is required." });
        if (string.IsNullOrWhiteSpace(request.ItemDescription))
            return BadRequest(new { message = "ItemDescription is required." });

        var report = new LostItemReport
        {
            ReporterName = request.ReporterName.Trim(),
            ReporterPhone = request.ReporterPhone.Trim(),
            ReporterEmail = request.ReporterEmail?.Trim(),
            ItemDescription = request.ItemDescription.Trim(),
            LastSeenLocation = request.LastSeenLocation?.Trim(),
            LostAtUtc = request.LostAtUtc,
            PhotoUrl = request.PhotoUrl?.Trim(),
            Status = "Pending",
            CreatedByUserId = GetCurrentUserId()
        };

        _context.LostItemReports.Add(report);
        await _context.SaveChangesAsync();
        return Ok(report);
    }

    [HttpGet("lost-items")]
    public async Task<IActionResult> GetLostItemReports([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _context.LostItemReports.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.Status == status.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("lost-items/{id:long}")]
    public async Task<IActionResult> GetLostItemReport(long id)
    {
        var item = await _context.LostItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Lost item report not found." });
        return Ok(item);
    }

    [HttpPatch("lost-items/{id:long}/close")]
    public async Task<IActionResult> CloseLostItemReport(long id)
    {
        var item = await _context.LostItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Lost item report not found." });

        item.Status = "Closed";
        item.ClosedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("found-items")]
    public async Task<IActionResult> CreateFoundItemReport([FromBody] FoundItemReportRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FoundByName))
            return BadRequest(new { message = "FoundByName is required." });
        if (string.IsNullOrWhiteSpace(request.FoundLocation))
            return BadRequest(new { message = "FoundLocation is required." });
        if (string.IsNullOrWhiteSpace(request.ItemDescription))
            return BadRequest(new { message = "ItemDescription is required." });

        var photoUrl = request.PhotoUrl;
        if (!string.IsNullOrWhiteSpace(request.PhotoBase64))
        {
            photoUrl = await _evidenceCapture.CaptureBase64Async(
                request.PhotoBase64, "LostFound", $"found-{DateTime.UtcNow:O}",
                createdByUserId: GetCurrentUserId());
        }

        var report = new FoundItemReport
        {
            FoundByName = request.FoundByName.Trim(),
            FoundLocation = request.FoundLocation.Trim(),
            FoundAtUtc = request.FoundAtUtc,
            ItemDescription = request.ItemDescription.Trim(),
            PhotoUrl = photoUrl?.Trim(),
            StorageLocation = request.StorageLocation?.Trim(),
            LockerCompartmentId = request.LockerCompartmentId,
            Status = "Unclaimed",
            CreatedByUserId = GetCurrentUserId()
        };

        _context.FoundItemReports.Add(report);
        await _context.SaveChangesAsync();
        return Ok(report);
    }

    [HttpGet("found-items")]
    public async Task<IActionResult> GetFoundItemReports([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = _context.FoundItemReports.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("found-items/{id:long}")]
    public async Task<IActionResult> GetFoundItemReport(long id)
    {
        var item = await _context.FoundItemReports
            .Include(f => f.LockerCompartment)
            .ThenInclude(c => c!.Cabinet)
            .FirstOrDefaultAsync(f => f.FoundItemReportId == id);

        if (item == null)
            return NotFound(new { message = "Found item report not found." });
        return Ok(item);
    }

    [HttpPost("match/suggestions")]
    public async Task<IActionResult> GetMatchSuggestions()
    {
        var suggestions = await _matchingService.GetSuggestionsAsync();
        return Ok(suggestions);
    }

    [HttpPost("match")]
    public async Task<IActionResult> CreateMatch([FromBody] CreateMatchRequest request)
    {
        if (!await _context.LostItemReports.AnyAsync(l => l.LostItemReportId == request.LostItemReportId))
            return BadRequest(new { message = "Lost item report not found." });
        if (!await _context.FoundItemReports.AnyAsync(f => f.FoundItemReportId == request.FoundItemReportId))
            return BadRequest(new { message = "Found item report not found." });

        var existing = await _context.ItemMatches.AnyAsync(m =>
            m.LostItemReportId == request.LostItemReportId &&
            m.FoundItemReportId == request.FoundItemReportId);
        if (existing)
            return BadRequest(new { message = "Match already exists." });

        var match = new ItemMatch
        {
            LostItemReportId = request.LostItemReportId,
            FoundItemReportId = request.FoundItemReportId,
            ConfidenceScore = request.ConfidenceScore,
            Status = "Suggested",
            MatchedByUserId = GetCurrentUserId(),
            Note = request.Note?.Trim()
        };

        _context.ItemMatches.Add(match);
        await _context.SaveChangesAsync();
        return Ok(match);
    }

    [HttpPost("match/{id:long}/confirm")]
    public async Task<IActionResult> ConfirmMatch(long id)
    {
        var success = await _matchingService.ConfirmMatchAsync(id, GetCurrentUserId() ?? 0);
        if (!success)
            return BadRequest(new { message = "Match not found or already processed." });
        return Ok(new { message = "Match confirmed." });
    }

    [HttpPost("match/{id:long}/reject")]
    public async Task<IActionResult> RejectMatch(long id)
    {
        var success = await _matchingService.RejectMatchAsync(id, GetCurrentUserId() ?? 0);
        if (!success)
            return BadRequest(new { message = "Match not found or already processed." });
        return Ok(new { message = "Match rejected." });
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches([FromQuery] string? status)
    {
        var query = _context.ItemMatches
            .Include(m => m.LostItem)
            .Include(m => m.FoundItem)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(m => m.Status == status.Trim());

        var matches = await query
            .OrderByDescending(m => m.MatchedAtUtc)
            .ToListAsync();

        return Ok(matches);
    }

    [HttpPost("claim-requests")]
    public async Task<IActionResult> CreateClaimRequest([FromBody] ClaimRequestRequest request)
    {
        if (!await _context.FoundItemReports.AnyAsync(f => f.FoundItemReportId == request.FoundItemReportId))
            return BadRequest(new { message = "Found item report not found." });
        if (string.IsNullOrWhiteSpace(request.ClaimantName))
            return BadRequest(new { message = "ClaimantName is required." });
        if (string.IsNullOrWhiteSpace(request.ClaimantIdNumber))
            return BadRequest(new { message = "ClaimantIdNumber is required." });

        var claim = new ClaimRequest
        {
            FoundItemReportId = request.FoundItemReportId,
            LostItemReportId = request.LostItemReportId,
            ClaimantName = request.ClaimantName.Trim(),
            ClaimantIdNumber = request.ClaimantIdNumber.Trim(),
            ClaimantPhone = request.ClaimantPhone?.Trim() ?? string.Empty,
            ProofDocumentUrl = request.ProofDocumentUrl?.Trim(),
            Status = "Pending"
        };

        _context.ClaimRequests.Add(claim);

        var found = await _context.FoundItemReports.FindAsync(request.FoundItemReportId);
        if (found != null) found.Status = "ClaimPending";

        await _context.SaveChangesAsync();
        return Ok(claim);
    }

    [HttpGet("claim-requests")]
    public async Task<IActionResult> GetClaimRequests([FromQuery] string? status)
    {
        var query = _context.ClaimRequests
            .Include(c => c.FoundItem)
            .Include(c => c.LostItem)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status.Trim());

        var claims = await query
            .OrderByDescending(c => c.RequestedAtUtc)
            .ToListAsync();

        return Ok(claims);
    }

    [HttpPatch("claim-requests/{id:long}/approve")]
    public async Task<IActionResult> ApproveClaimRequest(long id)
    {
        var claim = await _context.ClaimRequests
            .Include(c => c.FoundItem)
            .FirstOrDefaultAsync(c => c.ClaimRequestId == id);

        if (claim == null)
            return NotFound(new { message = "Claim request not found." });
        if (claim.Status != "Pending")
            return BadRequest(new { message = "Claim request is not pending." });

        claim.Status = "Approved";
        claim.ReviewedByUserId = GetCurrentUserId();
        claim.ReviewedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(claim);
    }

    [HttpPatch("claim-requests/{id:long}/complete")]
    public async Task<IActionResult> CompleteClaimRequest(long id)
    {
        var claim = await _context.ClaimRequests
            .Include(c => c.FoundItem)
            .ThenInclude(f => f!.LockerCompartment)
            .FirstOrDefaultAsync(c => c.ClaimRequestId == id);

        if (claim == null)
            return NotFound(new { message = "Claim request not found." });
        if (claim.Status != "Approved")
            return BadRequest(new { message = "Claim request is not approved." });

        claim.Status = "Completed";
        claim.CompletedAtUtc = DateTime.UtcNow;

        if (claim.FoundItem != null)
        {
            claim.FoundItem.Status = "Returned";
            claim.FoundItem.ReturnedAtUtc = DateTime.UtcNow;

            if (claim.FoundItem.LockerCompartmentId.HasValue)
            {
                await _lockerService.ReleaseCompartmentAsync(
                    claim.FoundItem.LockerCompartmentId.Value,
                    GetCurrentUserId() ?? 0);
            }
        }

        if (claim.LostItemReportId.HasValue)
        {
            var lost = await _context.LostItemReports.FindAsync(claim.LostItemReportId.Value);
            if (lost != null)
            {
                lost.Status = "Claimed";
                lost.ClosedAtUtc = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(claim);
    }

    [HttpPost("locker-cabinets")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLockerCabinet([FromBody] LockerCabinetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var cabinet = new LockerCabinet
        {
            Name = request.Name.Trim(),
            Location = request.Location?.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true
        };

        _context.LockerCabinets.Add(cabinet);
        await _context.SaveChangesAsync();
        return Ok(cabinet);
    }

    [HttpGet("locker-cabinets")]
    public async Task<IActionResult> GetLockerCabinets()
    {
        var cabinets = await _context.LockerCabinets
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(cabinets);
    }

    [HttpGet("locker-cabinets/{id:int}")]
    public async Task<IActionResult> GetLockerCabinetDetail(int id)
    {
        var cabinet = await _context.LockerCabinets.FindAsync(id);
        if (cabinet == null)
            return NotFound(new { message = "Cabinet not found." });

        var compartments = await _context.LockerCompartments
            .Where(c => c.LockerCabinetId == id)
            .Include(c => c.EvidenceItem)
            .OrderBy(c => c.Code)
            .ToListAsync();

        return Ok(new { cabinet, compartments });
    }

    [HttpPost("locker-cabinets/{id:int}/compartments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCompartments(int id, [FromBody] CreateCompartmentsRequest request)
    {
        if (!await _context.LockerCabinets.AnyAsync(c => c.LockerCabinetId == id))
            return NotFound(new { message = "Cabinet not found." });
        if (request.Codes == null || request.Codes.Count == 0)
            return BadRequest(new { message = "At least one code is required." });

        var created = new List<LockerCompartment>();
        foreach (var code in request.Codes)
        {
            var exists = await _context.LockerCompartments
                .AnyAsync(c => c.LockerCabinetId == id && c.Code == code.Trim());
            if (exists) continue;

            var compartment = new LockerCompartment
            {
                LockerCabinetId = id,
                Code = code.Trim().ToUpper(),
                Status = "Empty"
            };
            _context.LockerCompartments.Add(compartment);
            created.Add(compartment);
        }

        await _context.SaveChangesAsync();
        return Ok(created);
    }

    [HttpGet("compartments/available")]
    public async Task<IActionResult> GetAvailableCompartments([FromQuery] int? cabinetId)
    {
        if (cabinetId.HasValue)
        {
            var available = await _lockerService.GetAvailableCompartmentsAsync(cabinetId.Value);
            return Ok(available);
        }

        var all = await _context.LockerCompartments
            .Where(c => c.Status == "Empty")
            .Include(c => c.Cabinet)
            .OrderBy(c => c.LockerCabinetId)
            .ThenBy(c => c.Code)
            .ToListAsync();

        return Ok(all);
    }

    [HttpPost("compartments/{id:int}/assign")]
    public async Task<IActionResult> AssignCompartment(int id, [FromBody] AssignCompartmentRequest request)
    {
        var (success, message) = await _lockerService.AssignCompartmentAsync(
            id, request.EvidenceItemId, GetCurrentUserId() ?? 0);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("compartments/{id:int}/release")]
    public async Task<IActionResult> ReleaseCompartment(int id)
    {
        var (success, message) = await _lockerService.ReleaseCompartmentAsync(
            id, GetCurrentUserId() ?? 0);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet("access-logs")]
    public async Task<IActionResult> GetLockerAccessLogs([FromQuery] int? compartmentId, [FromQuery] int limit = 100)
    {
        var logs = await _lockerService.GetAccessLogsAsync(compartmentId, limit);
        return Ok(logs);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record LostItemReportRequest(string ReporterName, string ReporterPhone, string? ReporterEmail, string ItemDescription, string? LastSeenLocation, DateTime LostAtUtc, string? PhotoUrl);
    public sealed record FoundItemReportRequest(string FoundByName, string FoundLocation, DateTime FoundAtUtc, string ItemDescription, string? PhotoUrl, string? PhotoBase64, string? StorageLocation, int? LockerCompartmentId);
    public sealed record CreateMatchRequest(long LostItemReportId, long FoundItemReportId, double ConfidenceScore, string? Note);
    public sealed record ClaimRequestRequest(long FoundItemReportId, long? LostItemReportId, string ClaimantName, string ClaimantIdNumber, string? ClaimantPhone, string? ProofDocumentUrl);
    public sealed record LockerCabinetRequest(string Name, string? Location, string? Description);
    public sealed record CreateCompartmentsRequest(List<string> Codes);
    public sealed record AssignCompartmentRequest(long EvidenceItemId);
}
