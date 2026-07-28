using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Middleware;
using API.Services.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class FaceEnrollmentsController : ControllerBase
{
    private readonly IFaceEnrollmentService _service;
    public FaceEnrollmentsController(IFaceEnrollmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken token) => Ok(await _service.ListAsync(token));

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> Get(Guid jobId, CancellationToken token)
    {
        var value = await _service.GetAsync(jobId, token);
        return value is null ? NotFound() : Ok(value);
    }

    [HttpPost]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public async Task<IActionResult> Create(CreateFaceEnrollmentRequest request, CancellationToken token)
    {
        if (!TryUserId(out var userId)) return Unauthorized();
        try { return Accepted(await _service.CreateAsync(request, userId, token)); }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }

    [HttpPost("{jobId:guid}/cancel")]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<IActionResult> Cancel(Guid jobId, CancellationToken token) => Mutate(() => _service.CancelAsync(jobId, token));

    [HttpPost("{jobId:guid}/retry")]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<IActionResult> Retry(Guid jobId, CancellationToken token) => Mutate(() => _service.RetryAsync(jobId, token));

    [HttpPost("{jobId:guid}/activate")]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<IActionResult> Activate(Guid jobId, CancellationToken token) => Mutate(() => _service.ActivateAsync(jobId, token));

    private async Task<IActionResult> Mutate(Func<Task<FaceEnrollmentJobDto>> operation)
    {
        try { return Ok(await operation()); }
        catch (KeyNotFoundException e) { return NotFound(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
        catch (FaceRuntimeUnavailableException) { return StatusCode(503, new { message = "Face Runtime is unavailable." }); }
    }
    private bool TryUserId(out int id) => int.TryParse(
        User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
}
