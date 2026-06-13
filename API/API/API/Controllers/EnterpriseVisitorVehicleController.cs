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
[Route("api/enterprise/visitor-vehicle")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseVisitorVehicleController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IVisitorVehicleRiskScreeningService _screening;

    public EnterpriseVisitorVehicleController(ApplicationDbContext context, IVisitorVehicleRiskScreeningService screening)
    {
        _context = context;
        _screening = screening;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            Visits = await _context.Visits.CountAsync(),
            ActiveVisits = await _context.Visits.CountAsync(v => v.Status == VisitStatuses.CheckedIn),
            VisitorCredentials = await _context.VisitorCredentials.CountAsync(),
            WatchlistEntries = await _context.WatchlistEntries.CountAsync(e => e.IsActive),
            PendingWatchlistMatches = await _context.WatchlistMatches.CountAsync(m => m.Status == "Pending"),
            ParkingAreas = await _context.ParkingAreas.CountAsync(),
            ParkingPermits = await _context.ParkingPermits.CountAsync(p => !p.IsRevoked),
            Barriers = await _context.Barriers.CountAsync(),
            LaneEvents = await _context.LaneEvents.CountAsync()
        });
    }

    [HttpPost("visits")]
    public async Task<IActionResult> CreateVisit([FromBody] VisitRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VisitorName))
            return BadRequest(new { message = "VisitorName is required." });
        if (request.ExpectedOutUtc <= request.ExpectedInUtc)
            return BadRequest(new { message = "ExpectedOutUtc must be after ExpectedInUtc." });

        var visit = new Visit
        {
            SiteId = request.SiteId,
            HostEmployeeId = request.HostEmployeeId,
            VisitorName = request.VisitorName.Trim(),
            VisitorType = string.IsNullOrWhiteSpace(request.VisitorType) ? "Visitor" : request.VisitorType.Trim(),
            VisitorPhone = request.VisitorPhone?.Trim(),
            VisitorEmail = request.VisitorEmail?.Trim(),
            ExpectedInUtc = request.ExpectedInUtc,
            ExpectedOutUtc = request.ExpectedOutUtc,
            EscortRequired = request.EscortRequired,
            NdaRequired = request.NdaRequired,
            SafetyBriefingRequired = request.SafetyBriefingRequired,
            Status = VisitStatuses.Invited
        };

        _context.Visits.Add(visit);
        await _context.SaveChangesAsync();
        await CreateWatchlistMatchesForVisitAsync(visit);
        await _context.SaveChangesAsync();

        return Ok(visit);
    }

    [HttpPost("visits/{visitId:int}/credentials")]
    public async Task<IActionResult> IssueVisitorCredential(int visitId, [FromBody] VisitorCredentialRequest request)
    {
        var visit = await _context.Visits.FindAsync(visitId);
        if (visit == null)
            return NotFound(new { message = "Visit not found." });
        if (request.ValidToUtc <= request.ValidFromUtc)
            return BadRequest(new { message = "ValidToUtc must be after ValidFromUtc." });

        var credential = new VisitorCredential
        {
            VisitId = visitId,
            CredentialType = string.IsNullOrWhiteSpace(request.CredentialType) ? "QR" : request.CredentialType.Trim(),
            CredentialReference = string.IsNullOrWhiteSpace(request.CredentialReference)
                ? Guid.NewGuid().ToString("N")
                : request.CredentialReference.Trim(),
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc
        };

        _context.VisitorCredentials.Add(credential);
        await _context.SaveChangesAsync();
        return Ok(credential);
    }

    [HttpPost("visits/{visitId:int}/check-in")]
    public async Task<IActionResult> CheckInVisit(int visitId, [FromBody] VisitorCheckInRequest request)
    {
        var visit = await _context.Visits.FindAsync(visitId);
        if (visit == null)
            return NotFound(new { message = "Visit not found." });

        if (visit.NdaRequired)
        {
            var hasNda = await _context.VisitorFormAcceptances
                .Include(acceptance => acceptance.Template)
                .AnyAsync(acceptance => acceptance.VisitId == visitId && acceptance.Template!.FormType == "NDA");
            if (!hasNda)
                return BadRequest(new { message = "NDA acceptance is required before check-in." });
        }

        visit.Status = VisitStatuses.CheckedIn;
        visit.HostNotified = true;
        var checkIn = new VisitorCheckIn
        {
            VisitId = visitId,
            CheckedInAtUtc = DateTime.UtcNow,
            CheckedInByUserId = GetCurrentUserId(),
            IdDocumentType = request.IdDocumentType?.Trim(),
            IdDocumentReference = request.IdDocumentReference?.Trim(),
            VerificationStatus = string.IsNullOrWhiteSpace(request.VerificationStatus) ? "Verified" : request.VerificationStatus.Trim()
        };

        _context.VisitorCheckIns.Add(checkIn);
        await _context.SaveChangesAsync();
        return Ok(new { visit.VisitId, visit.Status, checkIn.VisitorCheckInId });
    }

    [HttpPost("visits/{visitId:int}/check-out")]
    public async Task<IActionResult> CheckOutVisit(int visitId)
    {
        var visit = await _context.Visits.FindAsync(visitId);
        if (visit == null)
            return NotFound(new { message = "Visit not found." });

        visit.Status = VisitStatuses.CheckedOut;
        var checkIn = await _context.VisitorCheckIns
            .Where(item => item.VisitId == visitId)
            .OrderByDescending(item => item.CheckedInAtUtc)
            .FirstOrDefaultAsync();
        if (checkIn != null)
        {
            checkIn.CheckedOutAtUtc = DateTime.UtcNow;
            checkIn.CheckedOutByUserId = GetCurrentUserId();
        }

        await _context.SaveChangesAsync();
        return Ok(new { visit.VisitId, visit.Status });
    }

    [HttpPost("forms")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateFormTemplate([FromBody] VisitorFormTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Body))
            return BadRequest(new { message = "Name and body are required." });

        var template = new VisitorFormTemplate
        {
            Name = request.Name.Trim(),
            FormType = string.IsNullOrWhiteSpace(request.FormType) ? "NDA" : request.FormType.Trim(),
            Version = request.Version <= 0 ? 1 : request.Version,
            Body = request.Body.Trim()
        };

        _context.VisitorFormTemplates.Add(template);
        await _context.SaveChangesAsync();
        return Ok(template);
    }

    [HttpPost("visits/{visitId:int}/form-acceptances")]
    public async Task<IActionResult> AcceptForm(int visitId, [FromBody] VisitorFormAcceptanceRequest request)
    {
        if (!await _context.Visits.AnyAsync(v => v.VisitId == visitId))
            return NotFound(new { message = "Visit not found." });
        if (!await _context.VisitorFormTemplates.AnyAsync(t => t.VisitorFormTemplateId == request.TemplateId))
            return BadRequest(new { message = "Template not found." });

        var acceptance = new VisitorFormAcceptance
        {
            VisitId = visitId,
            VisitorFormTemplateId = request.TemplateId,
            AcceptedByName = string.IsNullOrWhiteSpace(request.AcceptedByName) ? "Visitor" : request.AcceptedByName.Trim()
        };

        _context.VisitorFormAcceptances.Add(acceptance);
        await _context.SaveChangesAsync();
        return Ok(acceptance);
    }

    [HttpPost("watchlist")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateWatchlistEntry([FromBody] WatchlistEntryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest(new { message = "DisplayName is required." });

        var entry = new WatchlistEntry
        {
            EntityType = string.IsNullOrWhiteSpace(request.EntityType) ? "Person" : request.EntityType.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Identifier = request.Identifier?.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? "Medium" : request.Severity.Trim(),
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Security watchlist" : request.Reason.Trim()
        };

        _context.WatchlistEntries.Add(entry);
        await _context.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpPatch("watchlist-matches/{matchId:int}/review")]
    public async Task<IActionResult> ReviewWatchlistMatch(int matchId, [FromBody] WatchlistReviewRequest request)
    {
        var match = await _context.WatchlistMatches.FindAsync(matchId);
        if (match == null)
            return NotFound(new { message = "Match not found." });

        match.Status = string.IsNullOrWhiteSpace(request.Status) ? "Closed" : request.Status.Trim();
        match.ReviewNote = request.ReviewNote?.Trim();
        match.ReviewedAtUtc = DateTime.UtcNow;
        match.ReviewedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(match);
    }

    [HttpPost("parking-areas")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateParkingArea([FromBody] ParkingAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var area = new ParkingArea
        {
            SiteId = request.SiteId,
            Name = request.Name.Trim(),
            Capacity = request.Capacity
        };

        _context.ParkingAreas.Add(area);
        await _context.SaveChangesAsync();
        return Ok(area);
    }

    [HttpPost("parking-permits")]
    public async Task<IActionResult> CreateParkingPermit([FromBody] ParkingPermitRequest request)
    {
        if (!await _context.ParkingAreas.AnyAsync(area => area.ParkingAreaId == request.ParkingAreaId))
            return BadRequest(new { message = "Parking area not found." });
        if (request.ValidToUtc <= request.ValidFromUtc)
            return BadRequest(new { message = "ValidToUtc must be after ValidFromUtc." });

        var permit = new ParkingPermit
        {
            ParkingAreaId = request.ParkingAreaId,
            VehicleId = request.VehicleId,
            VisitId = request.VisitId,
            PermitType = string.IsNullOrWhiteSpace(request.PermitType) ? "Temporary" : request.PermitType.Trim(),
            ValidFromUtc = request.ValidFromUtc,
            ValidToUtc = request.ValidToUtc
        };

        _context.ParkingPermits.Add(permit);
        await _context.SaveChangesAsync();
        return Ok(permit);
    }

    [HttpPost("barriers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBarrier([FromBody] BarrierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        var barrier = new SecurityBarrier
        {
            LaneId = request.LaneId,
            Name = request.Name.Trim(),
            State = string.IsNullOrWhiteSpace(request.State) ? "Closed" : request.State.Trim()
        };

        _context.Barriers.Add(barrier);
        await _context.SaveChangesAsync();
        return Ok(barrier);
    }

    [HttpPost("barriers/{barrierId:int}/commands")]
    [RequireStepUp(PrivilegedActions.DeviceConfiguration)]
    public async Task<IActionResult> RecordBarrierCommand(int barrierId, [FromBody] BarrierCommandRequest request)
    {
        var barrier = await _context.Barriers.FindAsync(barrierId);
        if (barrier == null)
            return NotFound(new { message = "Barrier not found." });
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest(new { message = "Reason is required for barrier commands." });

        var command = string.IsNullOrWhiteSpace(request.Command) ? "Open" : request.Command.Trim();
        barrier.State = command.Equals("Open", StringComparison.OrdinalIgnoreCase) ? "Open" :
            command.Equals("Close", StringComparison.OrdinalIgnoreCase) ? "Closed" :
            command.Equals("HoldOpen", StringComparison.OrdinalIgnoreCase) ? "HeldOpen" :
            command.Equals("LockClosed", StringComparison.OrdinalIgnoreCase) ? "LockedClosed" :
            barrier.State;

        var audit = new BarrierCommandAudit
        {
            BarrierId = barrierId,
            Command = command,
            Reason = request.Reason.Trim(),
            RequestedByUserId = GetCurrentUserId()
        };

        _context.BarrierCommandAudits.Add(audit);
        _context.SecurityEvents.Add(new SecurityEvent
        {
            SourceType = "BarrierCommand",
            SourceId = barrierId.ToString(),
            EventType = "BarrierManualCommand",
            Severity = command.Equals("Open", StringComparison.OrdinalIgnoreCase) ||
                       command.Equals("HoldOpen", StringComparison.OrdinalIgnoreCase) ||
                       command.Equals("LockClosed", StringComparison.OrdinalIgnoreCase)
                ? "High"
                : "Medium",
            Summary = $"Barrier {barrier.Name} command {command}: {audit.Reason}",
            OccurredAtUtc = audit.RequestedAtUtc
        });
        await _context.SaveChangesAsync();
        return Ok(audit);
    }

    [HttpPost("lane-events")]
    public async Task<IActionResult> RecordLaneEvent([FromBody] LaneEventRequest request)
    {
        var laneEvent = new LaneEvent
        {
            LaneId = request.LaneId,
            VehicleId = request.VehicleId,
            EventType = string.IsNullOrWhiteSpace(request.EventType) ? "VehicleSeen" : request.EventType.Trim(),
            Direction = string.IsNullOrWhiteSpace(request.Direction) ? "Entry" : request.Direction.Trim(),
            PlateText = request.PlateText?.Trim(),
            Note = request.Note?.Trim()
        };

        _context.LaneEvents.Add(laneEvent);
        await _context.SaveChangesAsync();
        return Ok(laneEvent);
    }

    private async Task CreateWatchlistMatchesForVisitAsync(Visit visit)
    {
        var entries = await _context.WatchlistEntries
            .Where(entry => entry.IsActive && entry.EntityType == "Person")
            .Where(entry =>
                entry.DisplayName == visit.VisitorName ||
                (!string.IsNullOrWhiteSpace(entry.Identifier) &&
                 (entry.Identifier == visit.VisitorPhone || entry.Identifier == visit.VisitorEmail)))
            .ToListAsync();

        foreach (var entry in entries)
        {
            _context.WatchlistMatches.Add(new WatchlistMatch
            {
                WatchlistEntryId = entry.WatchlistEntryId,
                VisitId = visit.VisitId,
                Status = "Pending"
            });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record VisitRequest(int? SiteId, int? HostEmployeeId, string VisitorName, string? VisitorType, string? VisitorPhone, string? VisitorEmail, DateTime ExpectedInUtc, DateTime ExpectedOutUtc, bool EscortRequired, bool NdaRequired, bool SafetyBriefingRequired);
    public sealed record VisitorCredentialRequest(string? CredentialType, string? CredentialReference, DateTime ValidFromUtc, DateTime ValidToUtc);
    public sealed record VisitorCheckInRequest(string? IdDocumentType, string? IdDocumentReference, string? VerificationStatus);
    public sealed record VisitorFormTemplateRequest(string Name, string? FormType, int Version, string Body);
    public sealed record VisitorFormAcceptanceRequest(int TemplateId, string? AcceptedByName);
    public sealed record WatchlistEntryRequest(string? EntityType, string DisplayName, string? Identifier, string? Severity, string? Reason);
    public sealed record WatchlistReviewRequest(string? Status, string? ReviewNote);
    public sealed record ParkingAreaRequest(int? SiteId, string Name, int? Capacity);
    public sealed record ParkingPermitRequest(int ParkingAreaId, int? VehicleId, int? VisitId, string? PermitType, DateTime ValidFromUtc, DateTime ValidToUtc);
    public sealed record BarrierRequest(int? LaneId, string Name, string? State);
    public sealed record BarrierCommandRequest(string? Command, string? Reason);
    public sealed record LaneEventRequest(int? LaneId, int? VehicleId, string? EventType, string? Direction, string? PlateText, string? Note);
}
