using Microsoft.Extensions.Primitives;
using Services.Interfaces.Auth;

namespace API.Middleware;

/// <summary>
/// API Key authentication middleware (BR-API-001).
/// Header: <c>X-Api-Key</c>. A request authenticates if its key matches
/// EITHER an entry in the <c>ApiKeys</c> configuration section (legacy)
/// OR an active per-installation credential issued by <c>POST /register</c>
/// (FR-005 union mode, spec 001).
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _validKeys;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        // Treat both ApiKeys and AdminApiKeys as authoritative here — admin
        // keys must be able to reach the downstream AdminAuthenticationMiddleware,
        // which then enforces the stricter admin-only partition for /api/admin/*.
        IEnumerable<string> apiKeys = configuration.GetSection("ApiKeys")
            .GetChildren().Select(c => c.Value!);
        IEnumerable<string> adminKeys = configuration.GetSection("AdminApiKeys")
            .GetChildren().Select(c => c.Value!);
        _validKeys = [.. apiKeys.Concat(adminKeys)
            .Where(v => !string.IsNullOrWhiteSpace(v))];
    }

    public async Task InvokeAsync(HttpContext context, IInstallationCredentialValidator validator)
    {
        // Swagger/OpenAPI, health check, version and /register are unauthenticated.
        // /register is the entry point that establishes authentication for a new
        // installation (spec 001 contracts/register.md § Authentication / middleware).
        string path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/version", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/register", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out StringValues providedKey))
        {
            await Write401(context);
            return;
        }

        string key = providedKey.ToString();
        if (_validKeys.Contains(key))
        {
            await _next(context);
            return;
        }

        // Union mode (FR-005): fall through to DB-issued installation credentials.
        int? installationId = await validator.ValidateAsync(key, context.RequestAborted);
        if (installationId is not null)
        {
            await _next(context);
            return;
        }

        await Write401(context);
    }

    private static async Task Write401(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "API key missing or invalid." });
    }
}
