using System.Text.Json;
using Core.Enums;
using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// Admin-facing per-installation operations (US3 of spec 001): list
/// installations, revoke a specific one. Per-credential operations
/// (issue, revoke-active) live on <see cref="IInstallationCredentialService"/>;
/// this service delegates to it inside the revoke transaction.
/// </summary>
public class InstallationService : IInstallationService
{
    private readonly IInstallationRepository _installations;
    private readonly IInstallationCredentialService _credentials;
    private readonly IInstallationCredentialValidator _validator;
    private readonly IAuditService _audit;
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;
    private readonly ILogger<InstallationService> _logger;

    public InstallationService(
        IInstallationRepository installations,
        IInstallationCredentialService credentials,
        IInstallationCredentialValidator validator,
        IAuditService audit,
        AppDbContext db,
        ILogger<InstallationService> logger,
        TimeProvider? time = null)
    {
        _installations = installations;
        _credentials = credentials;
        _validator = validator;
        _audit = audit;
        _db = db;
        _logger = logger;
        _time = time ?? TimeProvider.System;
    }

    public async Task<IReadOnlyList<Installation>> ListAsync(string? clientApp,
        InstallationStatus? status, CancellationToken ct = default)
    {
        IReadOnlyList<InstallationEntity> rows = await _installations
            .ListAsync(clientApp, status, ct).ConfigureAwait(false);
        return [.. rows.Select(MapToDomain)];
    }

    public async Task<RevokeResult> RevokeAsync(int installationId, int changedByUserId,
        CancellationToken ct = default)
    {
        if (installationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(installationId),
                "InstallationId must be a positive integer.");
        }

        InstallationEntity? install = await _installations
            .GetByIdAsync(installationId, ct).ConfigureAwait(false);
        if (install is null)
        {
            return new RevokeResult.NotFound();
        }

        if (install.Status == InstallationStatus.Revoked)
        {
            // Idempotent re-revoke: original timestamp, no mutation, no audit.
            // DateTime Kind is pinned to Utc by AppDbContext.ConfigureConventions,
            // so the read value carries the "...Z" wire format already.
            return new RevokeResult.Success(
                install.RevokedAt ?? install.RegisteredAt, WasFirstRevoke: false);
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        InstallationStatus previousStatus = install.Status;
        string previousValue = JsonSerializer.Serialize(new
        {
            installationId,
            status = previousStatus.ToString(),
            revokedAt = (DateTime?)null
        });
        string newValue = JsonSerializer.Serialize(new
        {
            installationId,
            status = InstallationStatus.Revoked.ToString(),
            revokedAt = now
        });

        await using IDbContextTransaction txn = await _db.Database
            .BeginTransactionAsync(ct).ConfigureAwait(false);

        install.Status = InstallationStatus.Revoked;
        install.RevokedAt = now;
        await _installations.UpdateAsync(install, ct).ConfigureAwait(false);

        await _credentials.RevokeActiveAsync(installationId, now, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        await _audit.LogUpdateAsync(AuditEntityType.Installation, installationId,
            changedByUserId, previousValue, newValue, notes: null, ct)
            .ConfigureAwait(false);

        await txn.CommitAsync(ct).ConfigureAwait(false);

        // R4: cache invalidation MUST follow the durable write so the next
        // ValidateAsync misses, queries the DB, sees Status=Revoked, and
        // returns null — well under the 5 s SC-004 ceiling.
        _validator.Invalidate(installationId);

        _logger.LogInformation(
            "Revoked installation {InstallationId} (requested by user {UserId})",
            installationId, changedByUserId);

        return new RevokeResult.Success(now, WasFirstRevoke: true);
    }

    private static Installation MapToDomain(InstallationEntity entity)
        => Installation.Restore(
            entity.Id, entity.ClientApp, entity.OsUserId, entity.MachineId,
            entity.InstallGuid, entity.AppVersion, entity.DescriptorJson,
            entity.RegisteredAt, entity.Status, entity.RevokedAt);
}
