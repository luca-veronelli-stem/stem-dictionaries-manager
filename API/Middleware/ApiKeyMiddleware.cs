namespace API.Middleware;

/// <summary>
/// Middleware per autenticazione API Key (BR-API-001).
/// Header: X-Api-Key. Chiavi configurate in appsettings.json.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _validKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        var keysSection = configuration.GetSection("ApiKeys");
        _validKeys = [.. keysSection.GetChildren()
            .Select(c => c.Value!)
            .Where(v => !string.IsNullOrWhiteSpace(v))];
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Swagger/OpenAPI senza autenticazione
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey) ||
            !_validKeys.Contains(providedKey.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "API Key mancante o non valida." });
            return;
        }

        await _next(context);
    }
}
