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
[Route("api/enterprise/lost-found")]
[Authorize]
[RequireOperationalTask("lost-found")]
public class EnterpriseLostFoundController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly LostFoundMatchingService _matchingService;
    private readonly LockerService _lockerService;
    private readonly EvidenceCaptureService _evidenceCapture;
    private readonly UserOperationalScopeService _scopeService;
    private readonly INotificationService _notificationService;

    public EnterpriseLostFoundController(
        ApplicationDbContext context,
        LostFoundMatchingService matchingService,
        LockerService lockerService,
        EvidenceCaptureService evidenceCapture,
        UserOperationalScopeService scopeService,
        INotificationService notificationService)
    {
        _context = context;
        _matchingService = matchingService;
        _lockerService = lockerService;
        _evidenceCapture = evidenceCapture;
        _scopeService = scopeService;
        _notificationService = notificationService;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

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
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.ReporterName))
            return BadRequest(new { message = "Vui lòng nhập tên người báo tin." });
        if (string.IsNullOrWhiteSpace(request.ReporterPhone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại người báo tin." });
        if (string.IsNullOrWhiteSpace(request.ReporterIdNumber))
            return BadRequest(new { message = "Vui lòng nhập số CCCD/CMND người báo tin." });
        if (string.IsNullOrWhiteSpace(request.ItemDescription))
            return BadRequest(new { message = "Vui lòng mô tả vật phẩm." });
        if (string.IsNullOrWhiteSpace(request.ReporterPhotoUrl) && string.IsNullOrWhiteSpace(request.ReporterPhotoBase64))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh người báo tin." });
        if (string.IsNullOrWhiteSpace(request.PhotoUrl) && string.IsNullOrWhiteSpace(request.ItemPhotoBase64))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh vật phẩm." });

        var itemPhotoUrl = request.PhotoUrl;
        if (!string.IsNullOrWhiteSpace(request.ItemPhotoBase64))
        {
            itemPhotoUrl = await _evidenceCapture.CaptureBase64Async(
                request.ItemPhotoBase64, "LostFoundLostItem", $"lost-item-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                createdByUserId: GetCurrentUserId());
        }

        var reporterPhotoUrl = request.ReporterPhotoUrl;
        if (!string.IsNullOrWhiteSpace(request.ReporterPhotoBase64))
        {
            reporterPhotoUrl = await _evidenceCapture.CaptureBase64Async(
                request.ReporterPhotoBase64, "LostFoundReporter", $"lost-reporter-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                createdByUserId: GetCurrentUserId());
        }

        var report = new LostItemReport
        {
            ReporterName = request.ReporterName.Trim(),
            ReporterPhone = request.ReporterPhone.Trim(),
            ReporterEmail = request.ReporterEmail?.Trim(),
            ReporterIdNumber = request.ReporterIdNumber?.Trim(),
            ReporterPhotoUrl = reporterPhotoUrl?.Trim(),
            ItemDescription = request.ItemDescription.Trim(),
            LastSeenLocation = request.LastSeenLocation?.Trim(),
            LostAtUtc = request.LostAtUtc,
            PhotoUrl = itemPhotoUrl?.Trim(),
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
        if (!await CanAccessLostFoundAsync())
            return Forbid();

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
        if (!await CanAccessLostFoundAsync())
            return Forbid();

        var item = await _context.LostItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy phiếu báo mất đồ." });
        return Ok(item);
    }

    [HttpPut("lost-items/{id:long}")]
    public async Task<IActionResult> UpdateLostItemReport(long id, [FromBody] LostItemReportRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var item = await _context.LostItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy phiếu báo mất đồ." });

        item.ReporterName = request.ReporterName.Trim();
        item.ReporterPhone = request.ReporterPhone.Trim();
        item.ReporterEmail = request.ReporterEmail?.Trim();
        item.ReporterIdNumber = request.ReporterIdNumber?.Trim();
        item.ItemDescription = request.ItemDescription.Trim();
        item.LastSeenLocation = request.LastSeenLocation?.Trim();
        item.LostAtUtc = request.LostAtUtc;
        item.PhotoUrl = await ResolvePhotoUrlAsync(
            request.ItemPhotoBase64, request.PhotoUrl, item.PhotoUrl, "LostFoundLostItem", $"lost-item-{id}",
            GetCurrentUserId());
        item.ReporterPhotoUrl = await ResolvePhotoUrlAsync(
            request.ReporterPhotoBase64, request.ReporterPhotoUrl, item.ReporterPhotoUrl, "LostFoundReporter", $"lost-reporter-{id}",
            GetCurrentUserId());
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("lost-items/{id:long}")]
    public async Task<IActionResult> DeleteLostItemReport(long id)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var item = await _context.LostItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy phiếu báo mất đồ." });
        _context.LostItemReports.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("lost-items/{id:long}/close")]
    public async Task<IActionResult> CloseLostItemReport(long id)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var item = await _context.LostItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy phiếu báo mất đồ." });

        item.Status = "Closed";
        item.ClosedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPost("found-items")]
    public async Task<IActionResult> CreateFoundItemReport([FromBody] FoundItemReportRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.FoundByName))
            return BadRequest(new { message = "Vui lòng nhập tên người nhặt được." });
        if (string.IsNullOrWhiteSpace(request.FoundByPhone))
            return BadRequest(new { message = "Vui lòng nhập số điện thoại người nhặt được." });
        if (string.IsNullOrWhiteSpace(request.FoundByIdNumber))
            return BadRequest(new { message = "Vui lòng nhập số CCCD/CMND người nhặt được." });
        if (string.IsNullOrWhiteSpace(request.FoundLocation))
            return BadRequest(new { message = "Vui lòng nhập nơi nhặt được." });
        if (string.IsNullOrWhiteSpace(request.ItemDescription))
            return BadRequest(new { message = "Vui lòng mô tả vật phẩm." });
        if (string.IsNullOrWhiteSpace(request.FinderPhotoUrl) && string.IsNullOrWhiteSpace(request.FinderPhotoBase64))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh người nhặt được." });
        if (string.IsNullOrWhiteSpace(request.PhotoUrl) && string.IsNullOrWhiteSpace(request.PhotoBase64))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh vật phẩm." });

        var photoCapture = await _evidenceCapture.CaptureBase64WithRecordAsync(
            request.PhotoBase64, "LostFoundFoundItem", $"found-item-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            createdByUserId: GetCurrentUserId());
        var photoUrl = photoCapture?.Url ?? request.PhotoUrl;
        var finderPhotoUrl = request.FinderPhotoUrl;
        if (!string.IsNullOrWhiteSpace(request.FinderPhotoBase64))
        {
            finderPhotoUrl = await _evidenceCapture.CaptureBase64Async(
                request.FinderPhotoBase64, "LostFoundFinder", $"found-finder-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                createdByUserId: GetCurrentUserId());
        }

        await using var tx = await _context.Database.BeginTransactionAsync();

        var report = new FoundItemReport
        {
            FoundByName = request.FoundByName.Trim(),
            FoundByPhone = request.FoundByPhone?.Trim(),
            FoundByIdNumber = request.FoundByIdNumber?.Trim(),
            FinderPhotoUrl = finderPhotoUrl?.Trim(),
            FoundLocation = request.FoundLocation.Trim(),
            FoundAtUtc = request.FoundAtUtc,
            ItemDescription = request.ItemDescription.Trim(),
            PhotoUrl = photoUrl?.Trim(),
            StorageLocation = request.StorageLocation?.Trim(),
            LockerCompartmentId = request.LockerCompartmentId,
            ItemEvidenceId = photoCapture?.EvidenceItemId,
            Status = "Unclaimed",
            CreatedByUserId = GetCurrentUserId()
        };

        _context.FoundItemReports.Add(report);
        await _context.SaveChangesAsync();

        if (request.LockerCompartmentId.HasValue)
        {
            var (success, message) = await _lockerService.AssignCompartmentToFoundItemAsync(
                request.LockerCompartmentId.Value,
                report.FoundItemReportId,
                report.ItemEvidenceId,
                GetCurrentUserId() ?? 0);

            if (!success)
            {
                await tx.RollbackAsync();
                return BadRequest(new { message });
            }
        }

        await tx.CommitAsync();
        return Ok(report);
    }

    [HttpGet("found-items")]
    public async Task<IActionResult> GetFoundItemReports([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

        var query = _context.FoundItemReports.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(f => f.Status == status.Trim());

        var total = await query.CountAsync();
        var items = await query
            .Include(f => f.LockerCompartment)
            .ThenInclude(c => c!.Cabinet)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("found-items/{id:long}")]
    public async Task<IActionResult> GetFoundItemReport(long id)
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

        var item = await _context.FoundItemReports
            .Include(f => f.LockerCompartment)
            .ThenInclude(c => c!.Cabinet)
            .FirstOrDefaultAsync(f => f.FoundItemReportId == id);

        if (item == null)
            return NotFound(new { message = "Không tìm thấy phiếu nhặt được đồ." });
        return Ok(item);
    }

    [HttpPut("found-items/{id:long}")]
    public async Task<IActionResult> UpdateFoundItemReport(long id, [FromBody] FoundItemReportRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var item = await _context.FoundItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy phiếu nhặt được đồ." });

        item.FoundByName = request.FoundByName.Trim();
        item.FoundByPhone = request.FoundByPhone?.Trim();
        item.FoundByIdNumber = request.FoundByIdNumber?.Trim();
        item.FoundLocation = request.FoundLocation.Trim();
        item.FoundAtUtc = request.FoundAtUtc;
        item.ItemDescription = request.ItemDescription.Trim();
        if (!string.IsNullOrWhiteSpace(request.PhotoBase64))
        {
            var updatedCapture = await _evidenceCapture.CaptureBase64WithRecordAsync(
                request.PhotoBase64, "LostFoundFoundItem", $"found-item-{id}",
                createdByUserId: GetCurrentUserId());
            item.PhotoUrl = updatedCapture?.Url ?? item.PhotoUrl;
            item.ItemEvidenceId = updatedCapture?.EvidenceItemId ?? item.ItemEvidenceId;
        }
        else if (!string.IsNullOrWhiteSpace(request.PhotoUrl))
        {
            item.PhotoUrl = request.PhotoUrl.Trim();
        }
        item.FinderPhotoUrl = await ResolvePhotoUrlAsync(
            request.FinderPhotoBase64, request.FinderPhotoUrl, item.FinderPhotoUrl, "LostFoundFinder", $"found-finder-{id}",
            GetCurrentUserId());
        item.StorageLocation = request.StorageLocation?.Trim();
        if (item.LockerCompartmentId != request.LockerCompartmentId)
        {
            if (item.LockerCompartmentId.HasValue)
            {
                var (releaseSuccess, releaseMessage) = await _lockerService.ReleaseCompartmentAsync(
                    item.LockerCompartmentId.Value, GetCurrentUserId() ?? 0);
                if (!releaseSuccess)
                    return BadRequest(new { message = releaseMessage });
            }

            item.LockerCompartmentId = request.LockerCompartmentId;
            if (request.LockerCompartmentId.HasValue)
            {
                var (assignSuccess, assignMessage) = await _lockerService.AssignCompartmentToFoundItemAsync(
                    request.LockerCompartmentId.Value,
                    item.FoundItemReportId,
                    item.ItemEvidenceId,
                    GetCurrentUserId() ?? 0);
                if (!assignSuccess)
                    return BadRequest(new { message = assignMessage });
            }
        }
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("found-items/{id:long}")]
    public async Task<IActionResult> DeleteFoundItemReport(long id)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var item = await _context.FoundItemReports.FindAsync(id);
        if (item == null)
            return NotFound(new { message = "Không tìm thấy phiếu nhặt được đồ." });
        _context.FoundItemReports.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("match/suggestions")]
    public async Task<IActionResult> GetMatchSuggestions()
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

        var suggestions = await _matchingService.GetSuggestionsAsync();
        return Ok(suggestions);
    }

    [HttpPost("match")]
    public async Task<IActionResult> CreateMatch([FromBody] CreateMatchRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        if (!await _context.LostItemReports.AnyAsync(l => l.LostItemReportId == request.LostItemReportId))
            return BadRequest(new { message = "Không tìm thấy phiếu báo mất đồ." });
        if (!await _context.FoundItemReports.AnyAsync(f => f.FoundItemReportId == request.FoundItemReportId))
            return BadRequest(new { message = "Không tìm thấy phiếu nhặt được đồ." });

        var existing = await _context.ItemMatches.AnyAsync(m =>
            m.LostItemReportId == request.LostItemReportId &&
            m.FoundItemReportId == request.FoundItemReportId);
        if (existing)
            return BadRequest(new { message = "Trùng khớp đã tồn tại." });

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
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var success = await _matchingService.ConfirmMatchAsync(id, GetCurrentUserId() ?? 0);
        if (!success)
            return BadRequest(new { message = "Không tìm thấy trùng khớp hoặc đã xử lý." });
        return Ok(new { message = "Đã xác nhận trùng khớp." });
    }

    [HttpPost("match/{id:long}/reject")]
    public async Task<IActionResult> RejectMatch(long id)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var success = await _matchingService.RejectMatchAsync(id, GetCurrentUserId() ?? 0);
        if (!success)
            return BadRequest(new { message = "Không tìm thấy trùng khớp hoặc đã xử lý." });
        return Ok(new { message = "Đã từ chối trùng khớp." });
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches([FromQuery] string? status)
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

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
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        if (!await _context.FoundItemReports.AnyAsync(f => f.FoundItemReportId == request.FoundItemReportId))
            return BadRequest(new { message = "Không tìm thấy phiếu nhặt được đồ." });
        if (string.IsNullOrWhiteSpace(request.ClaimantName))
            return BadRequest(new { message = "Vui lòng nhập tên người nhận." });
        if (string.IsNullOrWhiteSpace(request.ClaimantIdNumber))
            return BadRequest(new { message = "Vui lòng nhập số CCCD/CMND người nhận." });
        if (string.IsNullOrWhiteSpace(request.ClaimantPhotoUrl) && string.IsNullOrWhiteSpace(request.ClaimantPhotoBase64))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh người nhận." });
        if (string.IsNullOrWhiteSpace(request.ItemPhotoUrl) && string.IsNullOrWhiteSpace(request.ItemPhotoBase64))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh vật phẩm." });
        var hasActiveClaim = await _context.ClaimRequests.AnyAsync(c =>
            c.FoundItemReportId == request.FoundItemReportId &&
            (c.Status == "Pending" || c.Status == "Approved"));
        if (hasActiveClaim)
            return BadRequest(new { message = "Vật phẩm này đã có yêu cầu nhận đang hoạt động." });

        var claimantPhotoUrl = await ResolvePhotoUrlAsync(
            request.ClaimantPhotoBase64, request.ClaimantPhotoUrl, null, "LostFoundClaimant", $"claimant-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            GetCurrentUserId());
        var claimantItemPhotoUrl = await ResolvePhotoUrlAsync(
            request.ItemPhotoBase64, request.ItemPhotoUrl, null, "LostFoundClaimItem", $"claim-item-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            GetCurrentUserId());

        var claim = new ClaimRequest
        {
            FoundItemReportId = request.FoundItemReportId,
            LostItemReportId = request.LostItemReportId,
            ClaimantName = request.ClaimantName.Trim(),
            ClaimantIdNumber = request.ClaimantIdNumber.Trim(),
            ClaimantPhone = request.ClaimantPhone?.Trim() ?? string.Empty,
            ProofDocumentUrl = request.ProofDocumentUrl?.Trim(),
            ClaimantPhotoUrl = claimantPhotoUrl?.Trim(),
            ItemPhotoUrl = claimantItemPhotoUrl?.Trim(),
            Status = "Pending"
        };

        _context.ClaimRequests.Add(claim);

        var found = await _context.FoundItemReports.FindAsync(request.FoundItemReportId);
        if (found != null) found.Status = "ClaimPending";

        await _context.SaveChangesAsync();
        await _notificationService.NotifyEventAsync("Approval.LostFound.ClaimCreated",
            "Yêu cầu nhận đồ mới",
            $"Có yêu cầu nhận đồ mới cần xem xét.",
            "LostFound", claim.ClaimRequestId.ToString(),
            "/lost-found");
        return Ok(claim);
    }

    [HttpGet("claim-requests")]
    public async Task<IActionResult> GetClaimRequests([FromQuery] string? status)
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

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

    [HttpPut("claim-requests/{id:long}")]
    public async Task<IActionResult> UpdateClaimRequest(long id, [FromBody] ClaimRequestRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var claim = await _context.ClaimRequests
            .Include(c => c.FoundItem)
            .FirstOrDefaultAsync(c => c.ClaimRequestId == id);

        if (claim == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu nhận đồ." });
        if (claim.Status is "Approved" or "Completed")
            return BadRequest(new { message = "Yêu cầu nhận đồ đã được phê duyệt hoặc hoàn tất, không thể chỉnh sửa." });
        if (!await _context.FoundItemReports.AnyAsync(f => f.FoundItemReportId == request.FoundItemReportId))
            return BadRequest(new { message = "Không tìm thấy phiếu nhặt được đồ." });
        if (string.IsNullOrWhiteSpace(request.ClaimantName))
            return BadRequest(new { message = "Vui lòng nhập tên người nhận." });
        if (string.IsNullOrWhiteSpace(request.ClaimantIdNumber))
            return BadRequest(new { message = "Vui lòng nhập số CCCD/CMND người nhận." });
        if (string.IsNullOrWhiteSpace(request.ClaimantPhotoUrl) && string.IsNullOrWhiteSpace(request.ClaimantPhotoBase64) && string.IsNullOrWhiteSpace(claim.ClaimantPhotoUrl))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh người nhận." });
        if (string.IsNullOrWhiteSpace(request.ItemPhotoUrl) && string.IsNullOrWhiteSpace(request.ItemPhotoBase64) && string.IsNullOrWhiteSpace(claim.ItemPhotoUrl))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh vật phẩm." });

        var hasActiveClaim = await _context.ClaimRequests.AnyAsync(c =>
            c.ClaimRequestId != id &&
            c.FoundItemReportId == request.FoundItemReportId &&
            (c.Status == "Pending" || c.Status == "Approved"));
        if (hasActiveClaim)
            return BadRequest(new { message = "Vật phẩm này đã có yêu cầu nhận đang hoạt động." });

        var previousFoundItemId = claim.FoundItemReportId;
        claim.FoundItemReportId = request.FoundItemReportId;
        claim.LostItemReportId = request.LostItemReportId;
        claim.ClaimantName = request.ClaimantName.Trim();
        claim.ClaimantIdNumber = request.ClaimantIdNumber.Trim();
        claim.ClaimantPhone = request.ClaimantPhone?.Trim() ?? string.Empty;
        claim.ProofDocumentUrl = request.ProofDocumentUrl?.Trim();
        claim.ClaimantPhotoUrl = await ResolvePhotoUrlAsync(
            request.ClaimantPhotoBase64, request.ClaimantPhotoUrl, claim.ClaimantPhotoUrl, "LostFoundClaimant", $"claimant-update-{id}",
            GetCurrentUserId());
        claim.ItemPhotoUrl = await ResolvePhotoUrlAsync(
            request.ItemPhotoBase64, request.ItemPhotoUrl, claim.ItemPhotoUrl, "LostFoundClaimItem", $"claim-item-update-{id}",
            GetCurrentUserId());
        claim.Status = "Pending";
        claim.ReviewedByUserId = null;
        claim.ReviewedAtUtc = null;
        claim.ReviewNote = null;
        claim.RejectionReason = null;
        claim.CompletedAtUtc = null;
        claim.CompletedByUserId = null;
        claim.WitnessName = null;
        claim.HandoverNote = null;
        claim.ReturnPhotoUrl = null;

        if (previousFoundItemId != request.FoundItemReportId)
        {
            var previousFound = await _context.FoundItemReports.FindAsync(previousFoundItemId);
            if (previousFound != null && previousFound.Status == "ClaimPending")
            {
                var stillHasActiveClaims = await _context.ClaimRequests.AnyAsync(c =>
                    c.ClaimRequestId != id &&
                    c.FoundItemReportId == previousFoundItemId &&
                    (c.Status == "Pending" || c.Status == "Approved"));
                if (!stillHasActiveClaims)
                    previousFound.Status = "Unclaimed";
            }
        }

        var currentFound = await _context.FoundItemReports.FindAsync(request.FoundItemReportId);
        if (currentFound != null && currentFound.Status != "Returned")
            currentFound.Status = "ClaimPending";

        await _context.SaveChangesAsync();
        return Ok(claim);
    }

    [HttpDelete("claim-requests/{id:long}")]
    public async Task<IActionResult> CancelClaimRequest(long id)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var claim = await _context.ClaimRequests.FindAsync(id);
        if (claim == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu nhận đồ." });
        if (claim.Status == "Completed")
            return BadRequest(new { message = "Yêu cầu nhận đồ đã hoàn tất, không thể hủy." });

        claim.Status = "Cancelled";
        claim.ReviewedByUserId ??= GetCurrentUserId();
        claim.ReviewedAtUtc ??= DateTime.UtcNow;
        claim.RejectionReason = string.IsNullOrWhiteSpace(claim.RejectionReason)
            ? "Yêu cầu nhận đồ đã hủy."
            : claim.RejectionReason;

        var found = await _context.FoundItemReports.FindAsync(claim.FoundItemReportId);
        if (found != null && found.Status == "ClaimPending")
        {
            var hasActiveClaims = await _context.ClaimRequests.AnyAsync(c =>
                c.ClaimRequestId != id &&
                c.FoundItemReportId == claim.FoundItemReportId &&
                (c.Status == "Pending" || c.Status == "Approved"));
            if (!hasActiveClaims)
                found.Status = "Unclaimed";
        }

        await _context.SaveChangesAsync();
        await _notificationService.NotifyEventAsync("Approval.LostFound.ClaimCancelled",
            "Yêu cầu nhận đồ đã hủy",
            $"Yêu cầu nhận đồ đã bị hủy.",
            "LostFound", claim.ClaimRequestId.ToString(),
            "/lost-found");
        return Ok(claim);
    }

    [HttpPatch("claim-requests/{id:long}/approve")]
    public async Task<IActionResult> ApproveClaimRequest(long id, [FromBody] ReviewClaimRequest? request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var claim = await _context.ClaimRequests
            .Include(c => c.FoundItem)
            .FirstOrDefaultAsync(c => c.ClaimRequestId == id);

        if (claim == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu nhận đồ." });
        if (claim.Status != "Pending")
            return BadRequest(new { message = "Yêu cầu nhận đồ chưa ở trạng thái chờ xử lý." });

        claim.Status = "Approved";
        claim.ReviewedByUserId = GetCurrentUserId();
        claim.ReviewedAtUtc = DateTime.UtcNow;
        claim.ReviewNote = request?.Note?.Trim();

        await _context.SaveChangesAsync();
        await _notificationService.NotifyEventAsync("Approval.LostFound.ClaimApproved",
            "Yêu cầu nhận đồ đã được duyệt",
            $"Yêu cầu nhận đồ đã được duyệt, vui lòng đến quầy lễ tân nhận.",
            "LostFound", claim.ClaimRequestId.ToString(),
            "/lost-found");
        return Ok(claim);
    }

    [HttpPatch("claim-requests/{id:long}/reject")]
    public async Task<IActionResult> RejectClaim(long id, [FromBody] RejectClaimRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var claim = await _context.ClaimRequests.FindAsync(id);
        if (claim == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu nhận đồ." });
        if (claim.Status != "Pending")
            return BadRequest(new { message = "Yêu cầu nhận đồ chưa ở trạng thái chờ xử lý." });

        claim.Status = "Rejected";
        claim.ReviewedByUserId = GetCurrentUserId();
        claim.ReviewedAtUtc = DateTime.UtcNow;
        claim.RejectionReason = request.Reason?.Trim();

        var found = await _context.FoundItemReports.FindAsync(claim.FoundItemReportId);
        if (found != null && found.Status == "ClaimPending")
            found.Status = "Unclaimed";

        await _context.SaveChangesAsync();
        await _notificationService.NotifyEventAsync("Approval.LostFound.ClaimRejected",
            "Yêu cầu nhận đồ bị từ chối",
            $"Yêu cầu nhận đồ bị từ chối.",
            "LostFound", claim.ClaimRequestId.ToString(),
            "/lost-found");
        return Ok(claim);
    }

    [HttpPatch("claim-requests/{id:long}/complete")]
    public async Task<IActionResult> CompleteClaimRequest(long id, [FromBody] CompleteClaimRequestRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var claim = await _context.ClaimRequests
            .Include(c => c.FoundItem)
            .ThenInclude(f => f!.LockerCompartment)
            .FirstOrDefaultAsync(c => c.ClaimRequestId == id);

        if (claim == null)
            return NotFound(new { message = "Không tìm thấy yêu cầu nhận đồ." });
        if (claim.Status != "Approved")
            return BadRequest(new { message = "Yêu cầu nhận đồ chưa được phê duyệt." });
        if (string.IsNullOrWhiteSpace(request.HandoverNote))
            return BadRequest(new { message = "Vui lòng nhập ghi chú bàn giao." });
        if (string.IsNullOrWhiteSpace(request.ReturnPhotoUrl) && string.IsNullOrWhiteSpace(request.ReturnPhotoBase64))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh trả đồ." });

        claim.ClaimantPhotoUrl = await ResolvePhotoUrlAsync(
            request.ClaimantPhotoBase64, request.ClaimantPhotoUrl, claim.ClaimantPhotoUrl, "LostFoundClaimant", $"claimant-complete-{id}",
            GetCurrentUserId());
        var returnPhotoUrl = await ResolvePhotoUrlAsync(
            request.ReturnPhotoBase64, request.ReturnPhotoUrl, claim.ReturnPhotoUrl, "LostFoundReturn", $"return-{id}",
            GetCurrentUserId());
        if (string.IsNullOrWhiteSpace(claim.ClaimantPhotoUrl))
            return BadRequest(new { message = "Vui lòng đính kèm ảnh người nhận trước khi hoàn tất bàn giao." });

        claim.Status = "Completed";
        claim.CompletedAtUtc = DateTime.UtcNow;
        claim.CompletedByUserId = GetCurrentUserId();
        claim.WitnessName = request.WitnessName?.Trim();
        claim.HandoverNote = request.HandoverNote.Trim();
        claim.ReturnPhotoUrl = returnPhotoUrl?.Trim();

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

            if (claim.FoundItem.ItemEvidenceId.HasValue)
            {
                _context.ChainOfCustodyEntries.Add(new ChainOfCustodyEntry
                {
                    EvidenceItemId = claim.FoundItem.ItemEvidenceId.Value,
                    Action = "ReturnedToOwner",
                    ActorUserId = GetCurrentUserId(),
                    FromCustodian = claim.FoundItem.StorageLocation ?? claim.FoundItem.LockerCompartment?.Code,
                    ToCustodian = claim.ClaimantName,
                    Note = BuildHandoverNote(claim, request)
                });
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
        await _notificationService.NotifyEventAsync("Approval.LostFound.ClaimCompleted",
            "Bàn giao đồ hoàn tất",
            $"Việc bàn giao đồ đã hoàn tất.",
            "LostFound", claim.ClaimRequestId.ToString(),
            "/lost-found");
        return Ok(claim);
    }

    [HttpPut("locker-cabinets/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateLockerCabinet(int id, [FromBody] LockerCabinetRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var cabinet = await _context.LockerCabinets.FindAsync(id);
        if (cabinet == null)
            return NotFound(new { message = "Không tìm thấy tủ khóa." });

        cabinet.Name = request.Name.Trim();
        cabinet.Location = request.Location?.Trim();
        cabinet.Description = request.Description?.Trim();
        await _context.SaveChangesAsync();
        return Ok(cabinet);
    }

    [HttpDelete("locker-cabinets/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLockerCabinet(int id)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var cabinet = await _context.LockerCabinets.FindAsync(id);
        if (cabinet == null)
            return NotFound(new { message = "Không tìm thấy tủ khóa." });
        var compartments = await _context.LockerCompartments.Where(c => c.LockerCabinetId == id).ToListAsync();
        if (compartments.Any(c => c.Status != "Empty"))
            return BadRequest(new { message = "Không thể xóa tủ khóa khi còn ngăn đang chứa đồ." });
        _context.LockerCompartments.RemoveRange(compartments);
        _context.LockerCabinets.Remove(cabinet);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("locker-cabinets")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateLockerCabinet([FromBody] LockerCabinetRequest request)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Vui lòng nhập tên tủ khóa." });

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
        if (!await CanAccessLostFoundAsync())
            return Forbid();

        var cabinets = await _context.LockerCabinets
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(cabinets);
    }

    [HttpGet("locker-cabinets/{id:int}")]
    public async Task<IActionResult> GetLockerCabinetDetail(int id)
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

        var cabinet = await _context.LockerCabinets.FindAsync(id);
        if (cabinet == null)
            return NotFound(new { message = "Không tìm thấy tủ khóa." });

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
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        if (!await _context.LockerCabinets.AnyAsync(c => c.LockerCabinetId == id))
            return NotFound(new { message = "Không tìm thấy tủ khóa." });
        if (request.Codes == null || request.Codes.Count == 0)
            return BadRequest(new { message = "Vui lòng nhập ít nhất một mã ngăn." });

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
        if (!await CanAccessLostFoundAsync())
            return Forbid();

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
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var (success, message) = await _lockerService.AssignCompartmentAsync(
            id, request.EvidenceItemId, GetCurrentUserId() ?? 0);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("compartments/{id:int}/release")]
    public async Task<IActionResult> ReleaseCompartment(int id)
    {
        if (!await CanAccessLostFoundAsync(requireManage: true))
            return Forbid();

        var (success, message) = await _lockerService.ReleaseCompartmentAsync(
            id, GetCurrentUserId() ?? 0);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet("access-logs")]
    public async Task<IActionResult> GetLockerAccessLogs([FromQuery] int? compartmentId, [FromQuery] int limit = 100)
    {
        if (!await CanAccessLostFoundAsync())
            return Forbid();

        var logs = await _lockerService.GetAccessLogsAsync(compartmentId, limit);
        return Ok(logs);
    }

    private Task<bool> CanAccessLostFoundAsync(bool requireManage = false)
    {
        return _scopeService.CanAccessAsync(
            User,
            UserOperationalScopeService.TaskLostFound,
            requireManage: requireManage);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task<string?> ResolvePhotoUrlAsync(
        string? photoBase64,
        string? requestedUrl,
        string? currentUrl,
        string evidenceType,
        string sourceRef,
        int? currentUserId)
    {
        if (!string.IsNullOrWhiteSpace(photoBase64))
        {
            return await _evidenceCapture.CaptureBase64Async(
                photoBase64,
                evidenceType,
                sourceRef,
                createdByUserId: currentUserId);
        }

        if (!string.IsNullOrWhiteSpace(requestedUrl))
            return requestedUrl.Trim();

        return currentUrl?.Trim();
    }

    private static string BuildHandoverNote(ClaimRequest claim, CompleteClaimRequestRequest request)
    {
        var parts = new List<string>
        {
            $"Claimant: {claim.ClaimantName}",
            $"ID: {claim.ClaimantIdNumber}",
            $"Phone: {claim.ClaimantPhone}",
            $"Handover note: {request.HandoverNote.Trim()}"
        };

        if (!string.IsNullOrWhiteSpace(request.WitnessName))
            parts.Add($"Witness: {request.WitnessName.Trim()}");

        return string.Join(" | ", parts);
    }

    public sealed record LostItemReportRequest(
        string ReporterName,
        string ReporterPhone,
        string? ReporterEmail,
        string? ReporterIdNumber,
        string? ReporterPhotoUrl,
        string? ReporterPhotoBase64,
        string ItemDescription,
        string? LastSeenLocation,
        DateTime LostAtUtc,
        string? PhotoUrl,
        string? ItemPhotoBase64);
    public sealed record FoundItemReportRequest(
        string FoundByName,
        string? FoundByPhone,
        string? FoundByIdNumber,
        string? FinderPhotoUrl,
        string? FinderPhotoBase64,
        string FoundLocation,
        DateTime FoundAtUtc,
        string ItemDescription,
        string? PhotoUrl,
        string? PhotoBase64,
        string? StorageLocation,
        int? LockerCompartmentId);
    public sealed record CreateMatchRequest(long LostItemReportId, long FoundItemReportId, double ConfidenceScore, string? Note);
    public sealed record ClaimRequestRequest(
        long FoundItemReportId,
        long? LostItemReportId,
        string ClaimantName,
        string ClaimantIdNumber,
        string? ClaimantPhone,
        string? ProofDocumentUrl,
        string? ClaimantPhotoUrl,
        string? ClaimantPhotoBase64,
        string? ItemPhotoUrl,
        string? ItemPhotoBase64);
    public sealed record LockerCabinetRequest(string Name, string? Location, string? Description);
    public sealed record CreateCompartmentsRequest(List<string> Codes);
    public sealed record AssignCompartmentRequest(long EvidenceItemId);
    public sealed record RejectClaimRequest(string? Reason);
    public sealed record ReviewClaimRequest(string? Note);
    public sealed record CompleteClaimRequestRequest(string? ClaimantPhotoUrl, string? ClaimantPhotoBase64, string? ReturnPhotoUrl, string? ReturnPhotoBase64, string? WitnessName, string HandoverNote);
}
