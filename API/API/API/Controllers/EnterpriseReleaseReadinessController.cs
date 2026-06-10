using System.Security.Claims;
using API.Data;
using API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/enterprise/release-readiness")]
[Authorize(Roles = "Admin,BaoVe")]
public class EnterpriseReleaseReadinessController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EnterpriseReleaseReadinessController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        return Ok(new
        {
            QaTestRuns = await _context.QaTestRuns.CountAsync(),
            FailedQaRuns = await _context.QaTestRuns.CountAsync(run => run.Status == "Failed"),
            ReleaseCandidates = await _context.ReleaseCandidates.CountAsync(),
            ApprovedReleaseCandidates = await _context.ReleaseCandidates.CountAsync(candidate => candidate.Status == "Approved"),
            PendingRequiredGates = await _context.ReleaseGateChecks.CountAsync(gate => gate.Required && gate.Status != "Passed"),
            RunbookAcknowledgements = await _context.RunbookAcknowledgements.CountAsync()
        });
    }

    [HttpPost("qa-test-runs")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StartQaTestRun([FromBody] QaTestRunRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TestType))
            return BadRequest(new { message = "TestType is required." });

        var run = new QaTestRun
        {
            TestType = request.TestType.Trim(),
            Profile = string.IsNullOrWhiteSpace(request.Profile) ? "MediumCompany" : request.Profile.Trim(),
            Status = "Running",
            EvidenceReference = request.EvidenceReference?.Trim(),
            Notes = request.Notes?.Trim()
        };

        _context.QaTestRuns.Add(run);
        await _context.SaveChangesAsync();
        return Ok(run);
    }

    [HttpPatch("qa-test-runs/{qaTestRunId:long}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteQaTestRun(long qaTestRunId, [FromBody] QaCompletionRequest request)
    {
        var run = await _context.QaTestRuns.FindAsync(qaTestRunId);
        if (run == null)
            return NotFound(new { message = "QA test run not found." });

        run.PassedCount = request.PassedCount;
        run.FailedCount = request.FailedCount;
        run.Status = request.FailedCount == 0 ? "Passed" : "Failed";
        run.CompletedAtUtc = DateTime.UtcNow;
        run.EvidenceReference = request.EvidenceReference?.Trim() ?? run.EvidenceReference;
        run.Notes = request.Notes?.Trim() ?? run.Notes;
        await _context.SaveChangesAsync();
        return Ok(run);
    }

    [HttpPost("release-candidates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateReleaseCandidate([FromBody] ReleaseCandidateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Version))
            return BadRequest(new { message = "Version is required." });

        var candidate = new ReleaseCandidate
        {
            Version = request.Version.Trim(),
            Status = "Draft",
            MigrationId = request.MigrationId?.Trim(),
            BuildReference = request.BuildReference?.Trim(),
            CreatedByUserId = GetCurrentUserId()
        };

        _context.ReleaseCandidates.Add(candidate);
        await _context.SaveChangesAsync();
        return Ok(candidate);
    }

    [HttpPost("release-candidates/{releaseCandidateId:long}/gate-checks")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RecordReleaseGate(long releaseCandidateId, [FromBody] ReleaseGateCheckRequest request)
    {
        if (!await _context.ReleaseCandidates.AnyAsync(candidate => candidate.ReleaseCandidateId == releaseCandidateId))
            return NotFound(new { message = "Release candidate not found." });
        if (string.IsNullOrWhiteSpace(request.GateName))
            return BadRequest(new { message = "GateName is required." });

        var gate = new ReleaseGateCheck
        {
            ReleaseCandidateId = releaseCandidateId,
            GateName = request.GateName.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Passed" : request.Status.Trim(),
            Required = request.Required,
            EvidenceReference = request.EvidenceReference?.Trim(),
            Notes = request.Notes?.Trim(),
            VerifiedByUserId = GetCurrentUserId()
        };

        _context.ReleaseGateChecks.Add(gate);
        await _context.SaveChangesAsync();
        return Ok(gate);
    }

    [HttpPatch("release-candidates/{releaseCandidateId:long}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveReleaseCandidate(long releaseCandidateId)
    {
        var candidate = await _context.ReleaseCandidates.FindAsync(releaseCandidateId);
        if (candidate == null)
            return NotFound(new { message = "Release candidate not found." });

        var blockingGates = await _context.ReleaseGateChecks
            .Where(gate => gate.ReleaseCandidateId == releaseCandidateId && gate.Required && gate.Status != "Passed")
            .Select(gate => gate.GateName)
            .ToListAsync();

        if (blockingGates.Count > 0)
            return BadRequest(new { message = "Required release gates are not passed.", blockingGates });

        candidate.Status = "Approved";
        candidate.ApprovedAtUtc = DateTime.UtcNow;
        candidate.ApprovedByUserId = GetCurrentUserId();
        await _context.SaveChangesAsync();
        return Ok(candidate);
    }

    [HttpPost("runbook-acknowledgements")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AcknowledgeRunbook([FromBody] RunbookAcknowledgementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RunbookName) || string.IsNullOrWhiteSpace(request.RoleName))
            return BadRequest(new { message = "RunbookName and RoleName are required." });

        var acknowledgement = new RunbookAcknowledgement
        {
            RunbookName = request.RunbookName.Trim(),
            RoleName = request.RoleName.Trim(),
            AcknowledgedByUserId = GetCurrentUserId(),
            EvidenceReference = request.EvidenceReference?.Trim()
        };

        _context.RunbookAcknowledgements.Add(acknowledgement);
        await _context.SaveChangesAsync();
        return Ok(acknowledgement);
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public sealed record QaTestRunRequest(string TestType, string? Profile, string? EvidenceReference, string? Notes);
    public sealed record QaCompletionRequest(int PassedCount, int FailedCount, string? EvidenceReference, string? Notes);
    public sealed record ReleaseCandidateRequest(string Version, string? MigrationId, string? BuildReference);
    public sealed record ReleaseGateCheckRequest(string GateName, string? Status, bool Required, string? EvidenceReference, string? Notes);
    public sealed record RunbookAcknowledgementRequest(string RunbookName, string RoleName, string? EvidenceReference);
}
