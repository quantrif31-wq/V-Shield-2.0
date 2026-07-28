using API.Middleware;
using API.Services.FaceCredentialBindings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/FaceCredentialBindings")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class FaceCredentialBindingsController(IFaceCredentialBindingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FaceCredentialBindingDto>>> List(CancellationToken token) =>
        Ok(await service.ListAsync(token));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<FaceCredentialBindingDto>> Get(long id, CancellationToken token)
    {
        var result = await service.GetAsync(id, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<ActionResult<FaceCredentialBindingDto>> Create(
        CreateFaceCredentialBindingRequest request,
        CancellationToken token) =>
        Execute(() => service.CreateAsync(request, token), created: true);

    [HttpPost("{id:long}/revoke")]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<ActionResult<FaceCredentialBindingDto>> Revoke(
        long id,
        RevokeFaceCredentialBindingRequest request,
        CancellationToken token) =>
        Execute(() => service.RevokeAsync(id, request, token));

    private async Task<ActionResult<FaceCredentialBindingDto>> Execute(
        Func<Task<FaceCredentialBindingDto>> action, bool created = false)
    {
        try
        {
            var result = await action();
            return created ? CreatedAtAction(nameof(Get), new { id = result.Id }, result) : Ok(result);
        }
        catch (FaceCredentialBindingDomainException ex)
        {
            return StatusCode(ex.StatusCode, new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = ex.Code,
                Detail = ex.Message
            });
        }
    }
}

[ApiController]
[Route("api/Employees/{employeeId:int}/face-credential-binding")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class EmployeeFaceCredentialBindingController(IFaceCredentialBindingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FaceCredentialBindingDto>> Get(int employeeId, CancellationToken token)
    {
        var result = await service.GetByEmployeeAsync(employeeId, token);
        return result is null ? NotFound() : Ok(result);
    }
}

[ApiController]
[Route("api/Employees/{employeeId:int}/face-credential-candidates")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class EmployeeFaceCredentialCandidatesController(IFaceCredentialBindingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FaceCredentialCandidateDto>>> Get(
        int employeeId,
        CancellationToken token)
    {
        try
        {
            return Ok(await service.GetCandidatesAsync(employeeId, token));
        }
        catch (FaceCredentialBindingDomainException ex)
        {
            return StatusCode(ex.StatusCode, new ProblemDetails
            {
                Status = ex.StatusCode,
                Title = ex.Code,
                Detail = ex.Message
            });
        }
    }
}
