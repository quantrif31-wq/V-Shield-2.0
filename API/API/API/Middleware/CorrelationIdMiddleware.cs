namespace API.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = IsValidCorrelationId(incoming)
            ? incoming!.Trim()
            : Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }

    private static bool IsValidCorrelationId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Length <= 100 &&
               value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
    }
}
