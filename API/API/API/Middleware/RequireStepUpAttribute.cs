using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Middleware;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireStepUpAttribute : TypeFilterAttribute
{
    public RequireStepUpAttribute(string action)
        : base(typeof(RequireStepUpFilter))
    {
        Arguments = new object[] { action };
    }
}

public sealed class RequireStepUpFilter : IAsyncAuthorizationFilter
{
    public const string HeaderName = "X-Step-Up-Session-Id";
    private readonly string _action;
    private readonly IStepUpService _stepUpService;

    public RequireStepUpFilter(string action, IStepUpService stepUpService)
    {
        _action = action;
        _stepUpService = stepUpService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userIdClaim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var sessionId = ReadSessionId(context);
        if (!sessionId.HasValue)
        {
            Deny(context);
            return;
        }

        if (await _stepUpService.HasActiveSessionAsync(userId, _action, sessionId))
            return;

        Deny(context);
    }

    private void Deny(AuthorizationFilterContext context)
    {
        context.Result = new ObjectResult(new
        {
            code = "step_up_required",
            message = "Privileged action requires a fresh step-up verification.",
            action = _action,
            header = HeaderName
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };
    }

    private static long? ReadSessionId(AuthorizationFilterContext context)
    {
        if (context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var headerValue) &&
            long.TryParse(headerValue.FirstOrDefault(), out var headerSessionId))
        {
            return headerSessionId;
        }

        if (context.HttpContext.Request.Query.TryGetValue("stepUpSessionId", out var queryValue) &&
            long.TryParse(queryValue.FirstOrDefault(), out var querySessionId))
        {
            return querySessionId;
        }

        return null;
    }
}
