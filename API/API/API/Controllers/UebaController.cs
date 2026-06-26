using API.Models;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/ueba")]
[Authorize(Roles = "Admin,BaoVe,QuanLy")]
public class UebaController : ControllerBase
{
    private readonly IUebaService _ueba;
    private readonly IUebaRiskGraphService _riskGraph;

    public UebaController(IUebaService ueba, IUebaRiskGraphService riskGraph)
    {
        _ueba = ueba;
        _riskGraph = riskGraph;
    }

    [HttpGet("profiles")]
    public async Task<IActionResult> GetProfiles([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var profiles = await _ueba.GetProfilesAsync(page, pageSize);
        return Ok(profiles);
    }

    [HttpGet("profiles/{employeeId:int}")]
    public async Task<IActionResult> GetProfile(int employeeId)
    {
        var profile = await _ueba.GetProfileAsync(employeeId);
        if (profile == null)
            return NotFound(new { message = "Chua co profile cho nhan vien nay. Hay goi rebuild." });
        return Ok(profile);
    }

    [HttpPost("profiles/{employeeId:int}/rebuild")]
    public async Task<IActionResult> RebuildProfile(int employeeId)
    {
        var profile = await _ueba.BuildProfileAsync(employeeId);
        return Ok(new { message = "Da xay dung lai profile.", profile });
    }

    [HttpGet("anomalies")]
    public async Task<IActionResult> GetAnomalies(
        [FromQuery] int? employeeId, [FromQuery] string? type,
        [FromQuery] string? severity, [FromQuery] string? status,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate,
        [FromQuery] int maxResults = 50)
    {
        var anomalies = await _ueba.GetAnomaliesAsync(
            employeeId, type, severity, status, fromDate, toDate, maxResults);
        return Ok(anomalies);
    }

    [HttpPost("anomalies/{id:int}/resolve")]
    public async Task<IActionResult> ResolveAnomaly(int id, [FromBody] UebaResolveRequest request)
    {
        var currentUserId = GetCurrentEmployeeId();
        if (currentUserId == null)
            return Unauthorized();

        await _ueba.ResolveAnomalyAsync(id, request.Resolution, currentUserId.Value);
        return Ok(new { message = "UEBA anomaly resolved." });
    }

    [HttpPost("anomalies/{id:int}/false-positive")]
    public async Task<IActionResult> MarkFalsePositive(int id)
    {
        var currentUserId = GetCurrentEmployeeId();
        if (currentUserId == null)
            return Unauthorized();

        await _ueba.MarkFalsePositiveAsync(id, currentUserId.Value);
        return Ok(new { message = "UEBA anomaly marked as false positive." });
    }

    /// <summary>
    /// POST /api/ueba/employees/{employeeId}/risk-explanation - AI phan tich rui ro nhan su
    /// </summary>
    [HttpPost("employees/{employeeId:int}/risk-explanation")]
    public async Task<IActionResult> ExplainEmployeeRisk(int employeeId)
    {
        try
        {
            var result = await _riskGraph.ExplainEmployeeRiskAsync(employeeId, GetCurrentEmployeeId());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Employee not found." });
        }
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _ueba.GetSummaryAsync();
        return Ok(summary);
    }

    private int? GetCurrentEmployeeId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
            return null;
        return id;
    }
}

public class UebaResolveRequest
{
    public string Resolution { get; set; } = string.Empty;
}
