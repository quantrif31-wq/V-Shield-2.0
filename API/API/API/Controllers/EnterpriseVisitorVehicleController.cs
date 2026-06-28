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
[Authorize(Roles = "Admin,BaoVe,LeTan")]
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

    [HttpGet("reception/overview")]
    public async Task<IActionResult> GetReceptionOverview()
    {
        var now = DateTime.UtcNow;
        var startOfDay = now.Date;
        var endOfDay = startOfDay.AddDays(1);

        var pendingArrivalStatuses = new[] { VisitStatuses.Invited, VisitStatuses.Approved };
        var activeStatuses = new[] { VisitStatuses.CheckedIn, VisitStatuses.Overstay };

        return Ok(new
        {
            TodayVisits = await _context.Visits.CountAsync(v => v.ExpectedInUtc >= startOfDay && v.ExpectedInUtc < endOfDay),
            PendingArrivals = await _context.Visits.CountAsync(v => pendingArrivalStatuses.Contains(v.Status) && v.ExpectedInUtc >= startOfDay && v.ExpectedInUtc < endOfDay),
            ActiveVisitors = await _context.Visits.CountAsync(v => activeStatuses.Contains(v.Status)),
            OverdueVisitors = await _context.Visits.CountAsync(v => activeStatuses.Contains(v.Status) && v.ExpectedOutUtc < now),
            LateArrivalsNeedFollowUp = await _context.Visits.CountAsync(v => pendingArrivalStatuses.Contains(v.Status) && v.ExpectedInUtc < now),
            OpenSecurityRequests = await _context.ReceptionInteractions.CountAsync(i =>
                i.SecurityRequested &&
                (i.Status == ReceptionInteractionStatuses.Open ||
                 i.Status == ReceptionInteractionStatuses.InProgress ||
                 i.Status == ReceptionInteractionStatuses.Escalated)),
            LostFoundCases = await _context.LostItemReports.CountAsync(l => l.Status == "Pending") +
                             await _context.FoundItemReports.CountAsync(f => f.Status == "Unclaimed")
        });
    }

    [HttpGet("reception/board")]
    public async Task<IActionResult> GetReceptionBoard([FromQuery] string? search)
    {
        var now = DateTime.UtcNow;
        var startOfDay = now.Date;
        var endOfDay = startOfDay.AddDays(1);
        var pendingArrivalStatuses = new[] { VisitStatuses.Invited, VisitStatuses.Approved };
        var activeStatuses = new[] { VisitStatuses.CheckedIn, VisitStatuses.Overstay };

        var baseQuery = _context.Visits
            .AsNoTracking()
            .Include(v => v.HostEmployee)
            .Include(v => v.Site)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            baseQuery = baseQuery.Where(v =>
                v.VisitorName.Contains(keyword) ||
                (v.VisitorPhone != null && v.VisitorPhone.Contains(keyword)) ||
                (v.VisitorEmail != null && v.VisitorEmail.Contains(keyword)) ||
                (v.HostEmployee != null && v.HostEmployee.FullName.Contains(keyword)));
        }

        var arrivals = await baseQuery
            .Where(v => v.ExpectedInUtc >= startOfDay && v.ExpectedInUtc < endOfDay)
            .OrderBy(v => v.ExpectedInUtc)
            .Take(100)
            .ToListAsync();

        var overdue = await baseQuery
            .Where(v => activeStatuses.Contains(v.Status) && v.ExpectedOutUtc < now)
            .OrderBy(v => v.ExpectedOutUtc)
            .Take(50)
            .ToListAsync();

        var lateArrivals = await baseQuery
            .Where(v => pendingArrivalStatuses.Contains(v.Status) && v.ExpectedInUtc < now)
            .OrderBy(v => v.ExpectedInUtc)
            .Take(50)
            .ToListAsync();

        var activeVisitors = await baseQuery
            .Where(v => activeStatuses.Contains(v.Status))
            .OrderBy(v => v.ExpectedOutUtc)
            .Take(50)
            .ToListAsync();

        var recentInteractions = await _context.ReceptionInteractions
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        return Ok(new
        {
            arrivals,
            overdue,
            lateArrivals,
            activeVisitors,
            recentInteractions
        });
    }

    [HttpGet("reception/lost-found")]
    public async Task<IActionResult> GetReceptionLostFound([FromQuery] string? search)
    {
        var keyword = search?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Ok(new
            {
                lostItems = Array.Empty<object>(),
                foundItems = Array.Empty<object>()
            });
        }

        var lostItems = await _context.LostItemReports
            .AsNoTracking()
            .Where(item =>
                item.ReporterName.Contains(keyword) ||
                (item.ReporterPhone != null && item.ReporterPhone.Contains(keyword)) ||
                (item.ReporterIdNumber != null && item.ReporterIdNumber.Contains(keyword)) ||
                item.ItemDescription.Contains(keyword))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        var foundItems = await _context.FoundItemReports
            .AsNoTracking()
            .Where(item =>
                item.FoundByName.Contains(keyword) ||
                (item.FoundByPhone != null && item.FoundByPhone.Contains(keyword)) ||
                (item.FoundByIdNumber != null && item.FoundByIdNumber.Contains(keyword)) ||
                item.ItemDescription.Contains(keyword))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        return Ok(new { lostItems, foundItems });
    }

    [HttpGet("reception/interactions")]
    public async Task<IActionResult> GetReceptionInteractions([FromQuery] int? visitId, [FromQuery] string? status)
    {
        var query = _context.ReceptionInteractions.AsNoTracking().AsQueryable();
        if (visitId.HasValue)
            query = query.Where(item => item.VisitId == visitId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(item => item.Status == status);

        var items = await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(100)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("reception/interactions")]
    public async Task<IActionResult> CreateReceptionInteraction([FromBody] ReceptionInteractionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
            return BadRequest(new { message = "Summary is required." });

        if (request.VisitId.HasValue && !await _context.Visits.AnyAsync(v => v.VisitId == request.VisitId.Value))
            return BadRequest(new { message = "Visit not found." });

        if (request.LostItemReportId.HasValue && !await _context.LostItemReports.AnyAsync(item => item.LostItemReportId == request.LostItemReportId.Value))
            return BadRequest(new { message = "Lost item report not found." });

        if (request.FoundItemReportId.HasValue && !await _context.FoundItemReports.AnyAsync(item => item.FoundItemReportId == request.FoundItemReportId.Value))
            return BadRequest(new { message = "Found item report not found." });

        var interaction = new ReceptionInteraction
        {
            VisitId = request.VisitId,
            LostItemReportId = request.LostItemReportId,
            FoundItemReportId = request.FoundItemReportId,
            InteractionType = string.IsNullOrWhiteSpace(request.InteractionType) ? ReceptionInteractionTypes.VisitorSupport : request.InteractionType.Trim(),
            Summary = request.Summary.Trim(),
            DetailNote = request.DetailNote?.Trim(),
            ContactPersonName = request.ContactPersonName?.Trim(),
            ContactPersonPhone = request.ContactPersonPhone?.Trim(),
            RelatedVehiclePlate = request.RelatedVehiclePlate?.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? ReceptionInteractionStatuses.Open : request.Status.Trim(),
            SecurityRequested = request.SecurityRequested,
            ResolutionNote = request.ResolutionNote?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = GetCurrentUserId(),
            UpdatedByUserId = GetCurrentUserId()
        };

        _context.ReceptionInteractions.Add(interaction);
        await _context.SaveChangesAsync();
        return Ok(interaction);
    }

    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits(
        [FromQuery] string? status,
        [FromQuery] int? hostEmployeeId,
        [FromQuery] int? siteId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Visits.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status);
        if (hostEmployeeId.HasValue)
            query = query.Where(v => v.HostEmployeeId == hostEmployeeId);
        if (siteId.HasValue)
            query = query.Where(v => v.SiteId == siteId);
        if (dateFrom.HasValue)
            query = query.Where(v => v.ExpectedInUtc >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(v => v.ExpectedInUtc <= dateTo.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.VisitorName.Contains(search) || (v.VisitorPhone != null && v.VisitorPhone.Contains(search)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.ExpectedInUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(v => v.HostEmployee)
            .Include(v => v.Site)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("visits/{visitId:int}")]
    public async Task<IActionResult> GetVisitDetail(int visitId)
    {
        var visit = await _context.Visits
            .Include(v => v.HostEmployee)
            .Include(v => v.Site)
            .FirstOrDefaultAsync(v => v.VisitId == visitId);
        if (visit == null)
            return NotFound(new { message = "Visit not found." });

        var credentials = await _context.VisitorCredentials
            .Where(c => c.VisitId == visitId).ToListAsync();
        var checkIn = await _context.VisitorCheckIns
            .Where(c => c.VisitId == visitId).OrderByDescending(c => c.CheckedInAtUtc).FirstOrDefaultAsync();
        var formAcceptances = await _context.VisitorFormAcceptances
            .Include(a => a.Template)
            .Where(a => a.VisitId == visitId).ToListAsync();
        var latestParkingPermit = await _context.ParkingPermits
            .Include(p => p.ParkingArea)
            .Include(p => p.Vehicle)
            .Where(p => p.VisitId == visitId && !p.IsRevoked)
            .OrderByDescending(p => p.ValidToUtc)
            .FirstOrDefaultAsync();
        var latestLaneEvent = latestParkingPermit?.VehicleId == null
            ? null
            : await _context.LaneEvents
                .Include(e => e.Lane)
                .Where(e => e.VehicleId == latestParkingPermit.VehicleId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .FirstOrDefaultAsync();
        var interactions = await _context.ReceptionInteractions
            .Where(i => i.VisitId == visitId)
            .OrderByDescending(i => i.CreatedAtUtc)
            .Take(20)
            .ToListAsync();

        return Ok(new
        {
            visit,
            credentials,
            checkIn,
            formAcceptances,
            receptionContext = new
            {
                latestParkingPermit,
                latestLaneEvent,
                interactions,
                currentPresence = checkIn?.CheckedInAtUtc != null && checkIn.CheckedOutAtUtc == null ? "OnSite" : "OffSite"
            }
        });
    }

    [HttpGet("visits/overstays")]
    public async Task<IActionResult> GetOverstays()
    {
        var overstays = await _context.Visits
            .Where(v => v.Status == VisitStatuses.Overstay || (v.Status == VisitStatuses.CheckedIn && v.ExpectedOutUtc < DateTime.UtcNow))
            .OrderByDescending(v => v.ExpectedOutUtc)
            .Take(50)
            .Include(v => v.HostEmployee)
            .Include(v => v.Site)
            .ToListAsync();
        return Ok(overstays);
    }

    [HttpGet("watchlist-entries")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> GetWatchlistEntries([FromQuery] bool? active, [FromQuery] string? entityType)
    {
        var query = _context.WatchlistEntries.AsQueryable();
        if (active.HasValue)
            query = query.Where(e => e.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(e => e.EntityType == entityType);
        var items = await query.OrderByDescending(e => e.CreatedAtUtc).Take(100).ToListAsync();
        return Ok(items);
    }

    [HttpGet("watchlist-matches")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> GetWatchlistMatches(
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.WatchlistMatches
            .Include(m => m.WatchlistEntry)
            .Include(m => m.Visit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(m => m.Status == status);
        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(m => m.WatchlistEntry != null && m.WatchlistEntry.Severity == severity);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.MatchedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("forms")]
    public async Task<IActionResult> GetFormTemplates([FromQuery] string? formType)
    {
        var query = _context.VisitorFormTemplates.AsQueryable();
        if (!string.IsNullOrWhiteSpace(formType))
            query = query.Where(t => t.FormType == formType);
        var items = await query.OrderByDescending(t => t.Version).ToListAsync();
        return Ok(items);
    }

    [HttpGet("contractors")]
    public async Task<IActionResult> GetContractors(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Contractors.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.FullName.Contains(search) || c.Company.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.ContractToUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Employee)
            .Include(c => c.Site)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("contractors/{contractorId:int}")]
    public async Task<IActionResult> GetContractorDetail(int contractorId)
    {
        var contractor = await _context.Contractors
            .Include(c => c.Employee)
            .Include(c => c.Site)
            .FirstOrDefaultAsync(c => c.ContractorId == contractorId);
        if (contractor == null)
            return NotFound(new { message = "Contractor not found." });
        return Ok(contractor);
    }

    [HttpPost("contractors")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateContractor([FromBody] ContractorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Company))
            return BadRequest(new { message = "FullName and Company are required." });
        if (request.ContractToUtc <= request.ContractFromUtc)
            return BadRequest(new { message = "ContractToUtc must be after ContractFromUtc." });

        var contractor = new Contractor
        {
            EmployeeId = request.EmployeeId,
            FullName = request.FullName.Trim(),
            Company = request.Company.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            ContractFromUtc = request.ContractFromUtc,
            ContractToUtc = request.ContractToUtc,
            SiteId = request.SiteId,
            RequiredTraining = request.RequiredTraining?.Trim()
        };

        _context.Contractors.Add(contractor);
        await _context.SaveChangesAsync();
        return Ok(contractor);
    }

    [HttpPatch("contractors/{contractorId:int}/revoke")]
    [RequireStepUp(PrivilegedActions.UserAdministration)]
    public async Task<IActionResult> RevokeContractor(int contractorId, [FromBody] RevokeContractorRequest request)
    {
        var contractor = await _context.Contractors.FindAsync(contractorId);
        if (contractor == null)
            return NotFound(new { message = "Contractor not found." });

        contractor.Status = ContractorStatuses.Revoked;
        contractor.RevokedAtUtc = DateTime.UtcNow;
        contractor.RevokedByUserId = GetCurrentUserId();
        contractor.RevocationReason = request.Reason?.Trim();

        if (contractor.EmployeeId.HasValue)
        {
            var employee = await _context.Employees.FindAsync(contractor.EmployeeId);
            if (employee != null)
            {
                employee.LifecycleStatus = EmployeeLifecycleStates.ContractorExpired;
                employee.LifecycleUpdatedAtUtc = DateTime.UtcNow;
                employee.Status = false;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(contractor);
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
    [Authorize(Roles = "Admin,BaoVe")]
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
            VisitorFormTemplateId = request.TemplateId ?? 0,
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
    [Authorize(Roles = "Admin,BaoVe")]
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
    [Authorize(Roles = "Admin,BaoVe")]
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
    [Authorize(Roles = "Admin,BaoVe")]
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
    [Authorize(Roles = "Admin,BaoVe")]
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
    [Authorize(Roles = "Admin,BaoVe")]
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
    [Authorize(Roles = "Admin,BaoVe")]
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

    [HttpGet("parking-areas")]
    public async Task<IActionResult> GetParkingAreas([FromQuery] int? siteId)
    {
        var query = _context.ParkingAreas.AsQueryable();
        if (siteId.HasValue) query = query.Where(a => a.SiteId == siteId);
        return Ok(await query.OrderBy(a => a.Name).ToListAsync());
    }

    [HttpGet("parking-permits")]
    public async Task<IActionResult> GetParkingPermits(
        [FromQuery] int? parkingAreaId,
        [FromQuery] int? vehicleId,
        [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.ParkingPermits
            .Include(p => p.ParkingArea)
            .Include(p => p.Vehicle)
            .Include(p => p.Visit)
            .AsQueryable();
        if (parkingAreaId.HasValue) query = query.Where(p => p.ParkingAreaId == parkingAreaId);
        if (vehicleId.HasValue) query = query.Where(p => p.VehicleId == vehicleId);
        if (activeOnly == true) query = query.Where(p => !p.IsRevoked && p.ValidToUtc >= DateTime.UtcNow);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.ValidToUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("barriers")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> GetBarriers([FromQuery] int? laneId, [FromQuery] bool? active)
    {
        var query = _context.Barriers.Include(b => b.Lane).AsQueryable();
        if (laneId.HasValue) query = query.Where(b => b.LaneId == laneId);
        if (active.HasValue) query = query.Where(b => b.IsActive == active.Value);
        return Ok(await query.OrderBy(b => b.Name).ToListAsync());
    }

    [HttpGet("barriers/{barrierId:int}/commands")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> GetBarrierCommands(int barrierId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        if (!await _context.Barriers.AnyAsync(b => b.BarrierId == barrierId))
            return NotFound(new { message = "Barrier not found." });
        var query = _context.BarrierCommandAudits.Where(a => a.BarrierId == barrierId);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.RequestedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("lane-events")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> GetLaneEvents(
        [FromQuery] int? laneId,
        [FromQuery] int? vehicleId,
        [FromQuery] string? plateText,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.LaneEvents.Include(e => e.Lane).Include(e => e.Vehicle).AsQueryable();
        if (laneId.HasValue) query = query.Where(e => e.LaneId == laneId);
        if (vehicleId.HasValue) query = query.Where(e => e.VehicleId == vehicleId);
        if (!string.IsNullOrWhiteSpace(plateText)) query = query.Where(e => e.PlateText != null && e.PlateText.Contains(plateText));
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(e => e.OccurredAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("lane-health")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> GetLaneHealth()
    {
        var lanes = await _context.Lanes.Where(l => l.IsActive).ToListAsync();
        var now = DateTime.UtcNow;
        var health = new List<object>();
        foreach (var lane in lanes)
        {
            var lastEvent = await _context.LaneEvents
                .Where(e => e.LaneId == lane.LaneId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .FirstOrDefaultAsync();
            var barriers = await _context.Barriers.Where(b => b.LaneId == lane.LaneId).ToListAsync();
            var lastEventAge = lastEvent != null ? (now - lastEvent.OccurredAtUtc).TotalMinutes : (double?)null;
            var degraded = lastEventAge > 30 || barriers.Any(b => b.State == "Unknown" || b.State == "LockedClosed");
            health.Add(new
            {
                lane.LaneId, lane.Name, lane.Direction,
                BarrierCount = barriers.Count,
                Barriers = barriers.Select(b => new { b.BarrierId, b.Name, b.State }),
                LastEventAt = lastEvent?.OccurredAtUtc,
                LastEventAgeMinutes = lastEventAge,
                LastEventType = lastEvent?.EventType,
                LastPlateText = lastEvent?.PlateText,
                IsDegraded = degraded,
                Status = degraded ? "Degraded" : "Healthy"
            });
        }
        return Ok(health);
    }

    [HttpPost("barriers/{barrierId:int}/simulate")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> SimulateBarrierCommand(int barrierId, [FromBody] SimulateBarrierRequest request)
    {
        var barrier = await _context.Barriers.FindAsync(barrierId);
        if (barrier == null)
            return NotFound(new { message = "Barrier not found." });

        var command = string.IsNullOrWhiteSpace(request.Command) ? "Open" : request.Command.Trim();
        var previousState = barrier.State;
        barrier.State = command.Equals("Open", StringComparison.OrdinalIgnoreCase) ? "Open" :
            command.Equals("Close", StringComparison.OrdinalIgnoreCase) ? "Closed" :
            command.Equals("HoldOpen", StringComparison.OrdinalIgnoreCase) ? "HeldOpen" :
            command.Equals("LockClosed", StringComparison.OrdinalIgnoreCase) ? "LockedClosed" :
            command.Equals("Fault", StringComparison.OrdinalIgnoreCase) ? "Fault" :
            barrier.State;

        var audit = new BarrierCommandAudit
        {
            BarrierId = barrierId,
            Command = command,
            Reason = $"Simulation: {(request.Reason ?? "No reason")}",
            RequestedByUserId = GetCurrentUserId(),
            Result = "Simulated"
        };
        _context.BarrierCommandAudits.Add(audit);
        barrier.State = previousState; // revert after simulation
        await _context.SaveChangesAsync();
        return Ok(new { barrier.BarrierId, barrier.Name, SimulatedCommand = command, Result = "SimulatedOK", PreviousState = previousState });
    }

    [HttpGet("adjudications")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> GetAdjudications(
        [FromQuery] string? status,
        [FromQuery] string? aiSource,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AiAdjudicationItems.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(aiSource)) query = query.Where(a => a.AiSource == aiSource);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(a => a.CreatedAtUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { total, page, pageSize, items });
    }

    [HttpPatch("adjudications/{adjudicationId:int}/review")]
    [Authorize(Roles = "Admin,BaoVe")]
    public async Task<IActionResult> ReviewAdjudication(int adjudicationId, [FromBody] AdjudicationReviewRequest request)
    {
        var item = await _context.AiAdjudicationItems.FindAsync(adjudicationId);
        if (item == null) return NotFound(new { message = "Adjudication item not found." });
        item.Status = string.IsNullOrWhiteSpace(request.Status) ? "Reviewed" : request.Status.Trim();
        item.Outcome = request.Outcome?.Trim();
        item.ReviewNote = request.ReviewNote?.Trim();
        item.ReviewedByUserId = GetCurrentUserId();
        item.ReviewedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(item);
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
    public sealed record VisitorFormAcceptanceRequest(int? TemplateId, string? AcceptedByName);
    public sealed record WatchlistEntryRequest(string? EntityType, string DisplayName, string? Identifier, string? Severity, string? Reason);
    public sealed record WatchlistReviewRequest(string? Status, string? ReviewNote);
    public sealed record ParkingAreaRequest(int? SiteId, string Name, int? Capacity);
    public sealed record ParkingPermitRequest(int ParkingAreaId, int? VehicleId, int? VisitId, string? PermitType, DateTime ValidFromUtc, DateTime ValidToUtc);
    public sealed record BarrierRequest(int? LaneId, string Name, string? State);
    public sealed record BarrierCommandRequest(string? Command, string? Reason);
    public sealed record LaneEventRequest(int? LaneId, int? VehicleId, string? EventType, string? Direction, string? PlateText, string? Note);
    public sealed record ContractorRequest(int? EmployeeId, string FullName, string Company, string? Phone, string? Email, DateTime ContractFromUtc, DateTime ContractToUtc, int? SiteId, string? RequiredTraining);
    public sealed record RevokeContractorRequest(string? Reason);
    public sealed record SimulateBarrierRequest(string? Command, string? Reason);
    public sealed record AdjudicationReviewRequest(string? Status, string? Outcome, string? ReviewNote);
    public sealed record ReceptionInteractionRequest(
        int? VisitId,
        long? LostItemReportId,
        long? FoundItemReportId,
        string? InteractionType,
        string Summary,
        string? DetailNote,
        string? ContactPersonName,
        string? ContactPersonPhone,
        string? RelatedVehiclePlate,
        string? Status,
        bool SecurityRequested,
        string? ResolutionNote);
}
