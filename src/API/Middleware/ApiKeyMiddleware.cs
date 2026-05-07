using Microsoft.Extensions.Primitives;

namespace API.Middleware;

/// <summary>
/// API Key authentication middleware (BR-API-001).
/// Header: X-Api-Key. Keys are configured in appsettings.json.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _validKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        IConfigurationSection keysSection = configuration.GetSection("ApiKeys");
        _validKeys = [.. keysSection.GetChildren()
            .Select(c => c.Value!)
            .Where(v => !string.IsNullOrWhiteSpace(v))];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Swagger/OpenAPI, health check and version are unauthenticated
        string path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/version", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out StringValues providedKey) ||
            !_validKeys.Contains(providedKey.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "API key missing or invalid." });
            return;
        }

        await _next(context);
    }
}
