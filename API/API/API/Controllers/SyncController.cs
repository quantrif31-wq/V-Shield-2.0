using API.Services.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/sync")]
[AllowAnonymous]
public class SyncController : ControllerBase
{
    private readonly CentralSyncService _centralSyncService;

    public SyncController(CentralSyncService centralSyncService)
    {
        _centralSyncService = centralSyncService;
    }

    [HttpPost("nodes/register")]
    public async Task<IActionResult> RegisterNode([FromBody] SyncRegistrationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var registrationKey = Request.Headers["X-VShield-Registration-Key"].ToString();
            var response = await _centralSyncService.RegisterNodeAsync(request, registrationKey, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("upstream/events")]
    public async Task<IActionResult> IngestEvents([FromBody] SyncBatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var node = await AuthorizeNodeAsync(cancellationToken);
            if (!string.Equals(node.AreaNodeId, request.AreaNodeId, StringComparison.Ordinal))
            {
                return Unauthorized(new { message = "Area node mismatch." });
            }

            var response = await _centralSyncService.IngestUpstreamBatchAsync(node, request, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("downstream/events")]
    public async Task<IActionResult> GetDownstreamEvents([FromQuery] long afterSequence = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            var node = await AuthorizeNodeAsync(cancellationToken);
            var response = await _centralSyncService.GetDownstreamFeedAsync(node, afterSequence, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("downstream/ack")]
    public async Task<IActionResult> AckDownstream([FromBody] SyncAckRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var node = await AuthorizeNodeAsync(cancellationToken);
            await _centralSyncService.RecordAckAsync(node, request.LastAcknowledgedOutboxEventId, cancellationToken);
            return Ok(new { acknowledged = request.LastAcknowledgedOutboxEventId });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("bootstrap/master-data")]
    public async Task<IActionResult> BootstrapMasterData(CancellationToken cancellationToken)
    {
        try
        {
            var node = await AuthorizeNodeAsync(cancellationToken);
            var response = await _centralSyncService.BuildBootstrapAsync(node, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private Task<API.Models.SyncAreaNode> AuthorizeNodeAsync(CancellationToken cancellationToken)
    {
        var areaNodeId = Request.Headers["X-VShield-Node-Id"].ToString();
        var nodeSecret = Request.Headers["X-VShield-Node-Secret"].ToString();
        return _centralSyncService.ValidateNodeAsync(areaNodeId, nodeSecret, cancellationToken);
    }
}
