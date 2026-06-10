using System.Text.Json;

namespace API.Middleware;

public class SafeExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SafeExceptionHandlingMiddleware> _logger;

    public SafeExceptionHandlingMiddleware(RequestDelegate next, ILogger<SafeExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString()
                                ?? context.TraceIdentifier;

            _logger.LogError(ex,
                "Unhandled API exception. CorrelationId={CorrelationId} Path={Path}",
                correlationId,
                context.Request.Path.Value);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            context.Response.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

            var payload = new
            {
                type = "https://httpstatuses.com/500",
                title = "Internal Server Error",
                status = StatusCodes.Status500InternalServerError,
                detail = "An unexpected error occurred while processing the request.",
                correlationId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
