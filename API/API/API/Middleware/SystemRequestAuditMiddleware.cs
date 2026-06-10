using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
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
        var isAuditableApiRequest = IsAuditableApiRequest(context.Request.Path.Value, method);
        string? loginUsername = null;

        if (IsLoginRequest(context))
        {
            loginUsername = await TryReadLoginUsername(context);
        }

        if (!isAuditableApiRequest)
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
                var username = ResolveUsername(context, loginUsername);
                var statusCode = context.Response?.StatusCode ?? 0;
                var success = failureReason == null && statusCode is >= 200 and < 400;
                var requestMeta = BuildRequestMeta(context);
                var category = ResolveCategory(context.Request.Path.Value, method);

                var log = new SystemAuditLog
                {
                    TimestampUtc = DateTime.UtcNow,
                    UserId = userId,
                    Username = username,
                    CorrelationId = requestMeta.CorrelationId,
                    EventCategory = category,
                    Severity = ResolveSeverity(success, statusCode),
                    ClientIp = requestMeta.Ip,
                    UserAgent = requestMeta.UserAgent,
                    HttpMethod = method,
                    Path = context.Request.Path.Value,
                    ActionType = "REQUEST",
                    EntityName = null,
                    EntityId = null,
                    OldValuesJson = null,
                    NewValuesJson = JsonSerializer.Serialize(requestMeta),
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

    private static bool IsLoginRequest(HttpContext context)
    {
        return string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(context.Request.Path.Value, "/api/Auth/login", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAuditableApiRequest(string? path, string method)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase)) return false;
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> TryReadLoginUsername(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);
            context.Request.Body.Position = 0;

            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (!doc.RootElement.TryGetProperty("username", out var u)) return null;
            return u.GetString();
        }
        catch
        {
            if (context.Request.Body.CanSeek) context.Request.Body.Position = 0;
            return null;
        }
    }

    private static string? ResolveUsername(HttpContext context, string? loginUsername)
    {
        var user = context.User;
        return user?.Identity?.Name
            ?? user?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value
            ?? user?.FindFirst("unique_name")?.Value
            ?? user?.FindFirst(ClaimTypes.Name)?.Value
            ?? user?.FindFirst("username")?.Value
            ?? user?.FindFirst("preferred_username")?.Value
            ?? loginUsername
            ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }

    private static string ResolveCategory(string? path, string method)
    {
        if (path?.StartsWith("/api/Auth", StringComparison.OrdinalIgnoreCase) == true)
            return "AUTH";

        if (path?.Contains("access", StringComparison.OrdinalIgnoreCase) == true ||
            path?.Contains("qr", StringComparison.OrdinalIgnoreCase) == true ||
            path?.Contains("gate", StringComparison.OrdinalIgnoreCase) == true ||
            path?.Contains("video", StringComparison.OrdinalIgnoreCase) == true ||
            path?.Contains("biometric", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "SECURITY";
        }

        return method is "POST" or "PUT" or "PATCH" or "DELETE" ? "DATA_CHANGE" : "APPLICATION";
    }

    private static string ResolveSeverity(bool success, int statusCode)
    {
        if (success) return "INFO";
        if (statusCode is 401 or 403) return "WARN";
        if (statusCode >= 500) return "ERROR";
        return "WARN";
    }

    private static RequestAuditMeta BuildRequestMeta(HttpContext context)
    {
        var headers = context.Request.Headers;
        var forwardedFor = headers.TryGetValue("X-Forwarded-For", out var xff) ? xff.ToString() : null;
        var realIp = headers.TryGetValue("X-Real-IP", out var xri) ? xri.ToString() : null;
        var country = headers.TryGetValue("CF-IPCountry", out var cfc) ? cfc.ToString() : null;
        var city = headers.TryGetValue("X-AppEngine-City", out var cityHeader) ? cityHeader.ToString() : null;

        var ip = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = headers.UserAgent.ToString();
        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                            ?? headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault()
                            ?? context.TraceIdentifier;

        return new RequestAuditMeta
        {
            CorrelationId = correlationId,
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent,
            Ip = ip,
            ForwardedFor = forwardedFor,
            RealIp = realIp,
            Country = country,
            City = city,
            Referer = headers.Referer.ToString(),
            Origin = headers.Origin.ToString()
        };
    }

    private sealed class RequestAuditMeta
    {
        public string? CorrelationId { get; set; }
        public string? UserAgent { get; set; }
        public string? Ip { get; set; }
        public string? ForwardedFor { get; set; }
        public string? RealIp { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Referer { get; set; }
        public string? Origin { get; set; }
    }
}
