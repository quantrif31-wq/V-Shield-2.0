using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Middleware;

public sealed class RequireOperationalTaskAttribute : TypeFilterAttribute
{
    public RequireOperationalTaskAttribute(string taskKey, bool requireManage = false)
        : base(typeof(RequireOperationalTaskFilter))
    {
        Arguments = [taskKey, requireManage];
    }
}

public sealed class RequireOperationalTaskFilter : IAsyncAuthorizationFilter
{
    private readonly string _taskKey;
    private readonly bool _requireManage;
    private readonly UserOperationalScopeService _scopeService;

    public RequireOperationalTaskFilter(string taskKey, bool requireManage, UserOperationalScopeService scopeService)
    {
        _taskKey = taskKey;
        _requireManage = requireManage;
        _scopeService = scopeService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var allowed = await _scopeService.CanAccessAsync(
            user,
            _taskKey,
            requireManage: _requireManage,
            cancellationToken: context.HttpContext.RequestAborted);

        if (!allowed)
            context.Result = new ForbidResult();
    }
}
