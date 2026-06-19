using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace API.RateLimiting;

/// <summary>
/// Per-API-key fixed-window rate limiting (#14). The partition key is the
/// <c>X-Api-Key</c> header value, so each consumer gets its own budget rather
/// than sharing one global bucket. The unauthenticated allow-list paths
/// (mirroring <see cref="API.Middleware.ApiKeyMiddleware"/>) are exempt, so
/// health/version probes and the <c>/register</c> entry point are never
/// throttled. Rejected requests get <c>429 Too Many Requests</c> (the default
/// rejection status is otherwise <c>503</c>).
/// </summary>
public static class ApiRateLimiting
{
    /// <summary>Requests permitted per key within <see cref="Window"/>.</summary>
    public const int PermitLimit = 100;

    /// <summary>Fixed window length applied per key.</summary>
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Registers the per-API-key global rate limiter.</summary>
    public static void AddApiRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter =
                PartitionedRateLimiter.Create<HttpContext, string>(CreatePartition);
        });
    }

    private static RateLimitPartition<string> CreatePartition(HttpContext httpContext)
    {
        string path = httpContext.Request.Path.Value ?? string.Empty;
        if (IsExempt(path))
        {
            return RateLimitPartition.GetNoLimiter("exempt");
        }

        string apiKey = httpContext.Request.Headers["X-Api-Key"].ToString();
        string partitionKey = string.IsNullOrEmpty(apiKey) ? "anonymous" : apiKey;
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitLimit,
                Window = Window
            });
    }

    private static bool IsExempt(string path) =>
        path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/version", StringComparison.OrdinalIgnoreCase)
        || path.Equals("/register", StringComparison.OrdinalIgnoreCase);
}
