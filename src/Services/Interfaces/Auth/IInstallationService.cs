using Core.Enums.Auth;
using Core.Models.Auth;

namespace Services.Interfaces.Auth;

/// <summary>
/// Discriminated result of <see cref="IInstallationService.RevokeAsync"/>.
/// Idempotent re-revoke returns <see cref="Success"/> with the original
/// <c>RevokedAt</c> and <c>WasFirstRevoke = false</c>, signalling that the
/// caller MUST NOT write a second audit row.
/// </summary>
public abstract record RevokeResult
{
    private RevokeResult() { }

    public sealed record Success(DateTime RevokedAt, bool WasFirstRevoke) : RevokeResult;

    public sealed record NotFound : RevokeResult;
}

/// <summary>
/// Admin-facing operations on per-installation identities (US3 of spec 001):
/// list installations, revoke a specific one. Per-credential operations
/// (issue, revoke-active) live on <see cref="IInstallationCredentialService"/>
/// and are an implementation detail of the <c>/register</c> flow.
/// </summary>
public interface IInstallationService
{
    /// <summary>
    /// Lists installations, optionally filtered by <paramref name="clientApp"/>
    /// (exact match) and/or <paramref name="status"/>. Filtering is
    /// server-side; the contract MUST NOT over-fetch and post-filter.
    /// </summary>
    Task<IReadOnlyList<Installation>> ListAsync(string? clientApp,
        InstallationStatus? status, CancellationToken ct = default);

    /// <summary>
    /// Atomically transitions <see cref="Installation"/>.<c>Status</c> and
    /// every owning <c>InstallationApiCredential.Status</c> from
    /// <c>Active</c> to <c>Revoked</c>, writes one
    /// <see cref="Core.Enums.AuditEntityType.Installation"/> audit row, and
    /// — after the commit succeeds — drops the validation cache entry for
    /// this installation. Idempotent: a second call on an already-revoked
    /// row returns <see cref="RevokeResult.Success"/> with
    /// <c>WasFirstRevoke = false</c> and does not mutate state or audit.
    /// </summary>
    Task<RevokeResult> RevokeAsync(int installationId, int changedByUserId,
        CancellationToken ct = default);
}
