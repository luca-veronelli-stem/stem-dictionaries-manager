using Infrastructure;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Services.Interfaces;

namespace API.Middleware;

/// <summary>
/// Admin gate for <c>/api/admin/*</c> paths (spec 001 § data-model.md
/// Audit split). Runs after <see cref="ApiKeyMiddleware"/>:
/// <list type="bullet">
///   <item>Non-admin paths: pass through.</item>
///   <item>Admin paths with a key in <c>AdminApiKeys</c>:
///     resolve the seeded <c>system-admin</c> user and stamp its id on
///     <see cref="ICurrentUserProvider.CurrentUserId"/> for downstream
///     audit attribution; pass through.</item>
///   <item>Admin paths with any other (still otherwise-valid) key: 401.
///     This is the "non-admin keys cannot reach <c>/api/admin/*</c>"
///     enforcement (FR-009).</item>
/// </list>
/// </summary>
public class AdminAuthenticationMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private const string AdminPathPrefix = "/api/admin/";
    private const string SystemAdminUsername = "system-admin";

    private readonly RequestDelegate _next;
    private readonly HashSet<string> _adminKeys;
    private readonly Lock _idLock = new();
    private int? _cachedSystemAdminId;

    public AdminAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;

        IConfigurationSection section = configuration.GetSection("AdminApiKeys");
        _adminKeys = [.. section.GetChildren()
            .Select(c => c.Value!)
            .Where(v => !string.IsNullOrWhiteSpace(v))];
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db,
        ICurrentUserProvider userProvider)
    {
        string path = context.Request.Path.Value ?? "";
        if (!path.StartsWith(AdminPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out StringValues providedKey) ||
            !_adminKeys.Contains(providedKey.ToString()))
        {
            // Same body as ApiKeyMiddleware so admin/non-admin failures are
            // indistinguishable from outside, per the admin endpoint contracts.
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "API key missing or invalid." });
            return;
        }

        int adminId = await ResolveSystemAdminIdAsync(db, context.RequestAborted);
        userProvider.CurrentUserId = adminId;

        await _next(context);
    }

    private async Task<int> ResolveSystemAdminIdAsync(AppDbContext db, CancellationToken ct)
    {
        lock (_idLock)
        {
            if (_cachedSystemAdminId is int hit)
            {
                return hit;
            }
        }

        UserEntity? user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == SystemAdminUsername, ct);
        if (user is null)
        {
            throw new InvalidOperationException(
                $"Required '{SystemAdminUsername}' user is missing — run migrations.");
        }

        lock (_idLock)
        {
            _cachedSystemAdminId ??= user.Id;
            return _cachedSystemAdminId.Value;
        }
    }
}
