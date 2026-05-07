using System.Text.Json;
using API.Dtos.Auth;
using Core.Enums;
using Core.Models.Auth;
using Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Services.Interfaces;
using Services.Interfaces.Auth;

namespace API.Endpoints.Auth;

/// <summary>
/// Admin-only endpoints under <c>/api/admin/*</c>. The
/// <see cref="API.Middleware.AdminAuthenticationMiddleware"/> gate enforces
/// <c>AdminApiKeys</c> auth and stamps the <c>system-admin</c> user id on
/// <see cref="ICurrentUserProvider"/>; per-endpoint code can then call the
/// audit service without re-resolving the actor.
/// </summary>
public static class AdminAuthEndpoints
{
    public static void MapAdminAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/admin/bootstrap-tokens", MintBootstrapToken)
            .WithName("AdminMintBootstrapToken")
            .WithTags("Admin");
    }

    private static async Task<IResult> MintBootstrapToken(
        MintBootstrapTokenRequestDto? request,
        IBootstrapTokenService tokens,
        IAuditService audit,
        ICurrentUserProvider currentUser,
        AppDbContext db,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ClientApp))
        {
            return Results.BadRequest(new { error = "clientApp is required" });
        }

        TimeSpan? ttl = request.TtlHours is int hours
            ? TimeSpan.FromHours(hours)
            : null;

        // Mint + audit travel together: rolling back the audit insert must
        // also undo the token row, so neither can show up in isolation.
        await using IDbContextTransaction txn = await db.Database
            .BeginTransactionAsync(ct).ConfigureAwait(false);

        BootstrapToken record;
        string plaintext;
        try
        {
            (record, plaintext) = await tokens.MintAsync(request.ClientApp, ttl, ct)
                .ConfigureAwait(false);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Results.BadRequest(new { error = "ttlHours out of range [1, 2160]" });
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new { error = "clientApp is required" });
        }

        // MintAsync has already SaveChanges'd to populate the entity Id;
        // because we're inside an open transaction, the row is staged but
        // not yet committed.

        int adminId = currentUser.CurrentUserId
            ?? throw new InvalidOperationException(
                "AdminAuthenticationMiddleware did not stamp CurrentUserId.");

        string newValueJson = JsonSerializer.Serialize(new
        {
            tokenId = record.Id,
            clientApp = record.ClientApp,
            mintedAt = record.MintedAt,
            expiresAt = record.ExpiresAt,
            status = record.Status.ToString()
        });
        await audit.LogCreateAsync(AuditEntityType.BootstrapToken, record.Id,
            adminId, newValueJson, notes: null, ct).ConfigureAwait(false);

        await txn.CommitAsync(ct).ConfigureAwait(false);

        return Results.Created($"/api/admin/bootstrap-tokens/{record.Id}",
            new MintBootstrapTokenResponseDto(
                TokenId: record.Id,
                ClientApp: record.ClientApp,
                Plaintext: plaintext,
                MintedAt: record.MintedAt,
                ExpiresAt: record.ExpiresAt));
    }
}
