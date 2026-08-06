using API.Middleware;
using API.Models;
using API.Services.AccessCredentials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/AccessCredentials")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class AccessCredentialsController(IAccessCredentialService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccessCredentialDto>>> List(
        [FromQuery] int? employeeId, CancellationToken token) =>
        Ok(await service.ListAsync(employeeId, token));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AccessCredentialDto>> Get(long id, CancellationToken token)
    {
        var result = await service.GetAsync(id, token);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public async Task<ActionResult<AccessCredentialDto>> Create(
        CreateAccessCredentialRequest request, CancellationToken token) =>
        await Execute(() => service.CreateAsync(request, token), created: true);

    [HttpPost("{id:long}/activate")]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<ActionResult<AccessCredentialDto>> Activate(
        long id, AccessCredentialTransitionRequest request, CancellationToken token) =>
        Execute(() => service.TransitionAsync(
            id, AccessCredentialStatuses.Active, request.RowVersion, request.Reason, token));

    [HttpPost("{id:long}/deactivate")]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<ActionResult<AccessCredentialDto>> Deactivate(
        long id, AccessCredentialTransitionRequest request, CancellationToken token) =>
        Execute(() => service.TransitionAsync(
            id, AccessCredentialStatuses.Inactive, request.RowVersion, request.Reason, token));

    [HttpPost("{id:long}/revoke")]
    [RequireOperationalTask("identity-mgmt", requireManage: true)]
    public Task<ActionResult<AccessCredentialDto>> Revoke(
        long id, AccessCredentialTransitionRequest request, CancellationToken token) =>
        Execute(() => service.TransitionAsync(
            id, AccessCredentialStatuses.Revoked, request.RowVersion, request.Reason, token));

    private async Task<ActionResult<AccessCredentialDto>> Execute(
        Func<Task<AccessCredentialDto>> action, bool created = false)
    {
        try
        {
            var result = await action();
            return created ? CreatedAtAction(nameof(Get), new { id = result.Id }, result) : Ok(result);
        }
        catch (AccessCredentialDomainException ex)
        {
            return StatusCode(ex.StatusCode, new ProblemDetails
            {
                Status = ex.StatusCode, Title = ex.Code, Detail = ex.Message
            });
        }
    }
}

[ApiController]
[Route("api/Employees/{employeeId:int}/access-credentials")]
[Authorize]
[RequireOperationalTask("identity-mgmt")]
public sealed class EmployeeAccessCredentialsController(IAccessCredentialService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccessCredentialDto>>> List(
        int employeeId, CancellationToken token) =>
        Ok(await service.ListAsync(employeeId, token));
}
