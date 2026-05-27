using System.IdentityModel.Tokens.Jwt;
using API.Data;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Middleware;

public class SystemRequestAuditMiddleware
{
    private readonly RequestDelegate _next;

    public SystemRequestAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ApplicationDbContext dbContext)
    {
        var method = context.Request.Method?.ToUpperInvariant() ?? "GET";
        var isMutation = method is "POST" or "PUT" or "PATCH" or "DELETE";
        if (!isMutation)
        {
            await _next(context);
            return;
        }

        string? failureReason = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            failureReason = ex.Message;
            throw;
        }
        finally
        {
            try
            {
                var userIdRaw = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                int? userId = int.TryParse(userIdRaw, out var parsed) ? parsed : null;
                var username = context.User?.Identity?.Name;
                var statusCode = context.Response?.StatusCode ?? 0;
                var success = failureReason == null && statusCode is >= 200 and < 400;

                var log = new SystemAuditLog
                {
                    TimestampUtc = DateTime.UtcNow,
                    UserId = userId,
                    Username = username,
                    HttpMethod = method,
                    Path = context.Request.Path.Value,
                    ActionType = "REQUEST",
                    EntityName = null,
                    EntityId = null,
                    OldValuesJson = null,
                    NewValuesJson = null,
                    IsSuccess = success,
                    FailureReason = success ? null : (failureReason ?? $"HTTP {statusCode}"),
                    StatusCode = statusCode
                };

                dbContext.SystemAuditLogs.Add(log);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
            }
        }
    }
}

