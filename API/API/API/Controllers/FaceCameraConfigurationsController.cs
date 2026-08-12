using API.Middleware;
using API.Services.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FaceCameraConfigurationsController : ControllerBase
{
    private readonly IFaceCameraConfigurationService _service;
    private readonly IFaceCameraSessionReconciler _reconciler;

    public FaceCameraConfigurationsController(
        IFaceCameraConfigurationService service,
        IFaceCameraSessionReconciler reconciler)
    {
        _service = service;
        _reconciler = reconciler;
    }

    [HttpGet]
    [RequireOperationalTask("monitoring")]
    public Task<FaceCameraConfigurationOverviewDto> GetAll(CancellationToken cancellationToken) =>
        _service.GetOverviewAsync(cancellationToken);

    [HttpGet("{runtimeCameraId}")]
    [RequireOperationalTask("monitoring")]
    public async Task<ActionResult<FaceCameraConfigurationDto>> Get(
        string runtimeCameraId,
        CancellationToken cancellationToken)
    {
        try
        {
            var configuration = await _service.GetAsync(runtimeCameraId, cancellationToken);
            return configuration is null ? NotFound() : Ok(configuration);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{runtimeCameraId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FaceCameraConfigurationDto>> Put(
        string runtimeCameraId,
        [FromBody] UpdateFaceCameraConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.UpsertAsync(runtimeCameraId, request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Cấu hình Face camera đã bị thay đổi bởi một yêu cầu khác." });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { message = "Cấu hình Face camera xung đột với bản ghi hiện có." });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{runtimeCameraId}/start")]
    [Authorize(Roles = "Admin")]
    public Task<ActionResult<FaceCameraDesiredStateDto>> Start(
        string runtimeCameraId,
        CancellationToken cancellationToken) =>
        ChangeDesiredState(runtimeCameraId, true, cancellationToken);

    [HttpPost("{runtimeCameraId}/stop")]
    [Authorize(Roles = "Admin")]
    public Task<ActionResult<FaceCameraDesiredStateDto>> Stop(
        string runtimeCameraId,
        CancellationToken cancellationToken) =>
        ChangeDesiredState(runtimeCameraId, false, cancellationToken);

    [HttpPost("reconcile")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FaceCameraReconcileResultDto>> Reconcile(
        CancellationToken cancellationToken) =>
        Ok(await _reconciler.ReconcileAsync(cancellationToken));

    private async Task<ActionResult<FaceCameraDesiredStateDto>> ChangeDesiredState(
        string runtimeCameraId,
        bool start,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = start
                ? await _service.StartAsync(runtimeCameraId, cancellationToken)
                : await _service.StopAsync(runtimeCameraId, cancellationToken);
            if (!result.RuntimeApplied && result.RuntimeStatusCode is null)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, result);
            }
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
