using API.Middleware;
using API.Services.FaceRecognition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/FaceModels")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class FaceModelsController : ControllerBase
{
    private readonly IFaceModelMetadataService _models;

    public FaceModelsController(IFaceModelMetadataService models)
    {
        _models = models;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FaceModelAdminDto>>> List(
        CancellationToken cancellationToken) =>
        Ok(await _models.ListAsync(null, cancellationToken));

    [HttpGet("health")]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var models = await _models.ListAsync(null, cancellationToken);
        return Ok(new
        {
            checkedAtUtc = DateTime.UtcNow,
            modelCount = models.Count,
            registryVersion = models.Select(model => model.RegistryVersion)
                .FirstOrDefault(version => version.HasValue),
            statusCounts = models
                .GroupBy(model => model.RegistrySyncState)
                .ToDictionary(group => group.Key, group => group.Count()),
            models
        });
    }
}

[ApiController]
[Route("api/Employees/{employeeId:int}/face-models")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class EmployeeFaceModelsController : ControllerBase
{
    private readonly IFaceModelMetadataService _models;

    public EmployeeFaceModelsController(IFaceModelMetadataService models)
    {
        _models = models;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FaceModelAdminDto>>> List(
        int employeeId,
        CancellationToken cancellationToken) =>
        Ok(await _models.ListAsync(employeeId, cancellationToken));
}
