using Core.Models.Auth;

namespace Services.Interfaces.Auth;

/// <summary>
/// Per-installation API credential service. US1 ships
/// <see cref="IssueAsync"/>; list and revoke arrive in US3.
/// </summary>
public interface IInstallationCredentialService
{
    /// <summary>
    /// Mints a fresh plaintext credential, hashes it, stages a new
    /// <see cref="InstallationApiCredential"/> row against the caller's shared
    /// <c>AppDbContext</c>, and returns both the domain record and the
    /// plaintext (which must be returned to the client exactly once and
    /// never logged — <c>data-model.md</c> invariant 4).
    /// </summary>
    Task<(InstallationApiCredential Record, string Plaintext)> IssueAsync(
        int installationId, DateTime issuedAt, CancellationToken ct = default);

    /// <summary>
    /// Flips every <c>Active</c> credential on the matched installation to
    /// <c>Revoked</c>, setting <c>RevokedAt = revokedAt</c>. Returns the
    /// number of rows flipped (0 when the installation has no active
    /// credentials; ≥ 1 in normal re-registration).
    /// </summary>
    /// <remarks>
    /// Does NOT mutate the parent <c>Installation</c> row — the
    /// Installation stays untouched. Does NOT call
    /// <c>AppDbContext.SaveChangesAsync</c>; the caller batches the revoke
    /// into the surrounding transaction (so the re-registration flow can
    /// commit revoke + issue + token-flip + audit atomically).
    /// Reusable by the future admin revoke endpoint (#68), which will
    /// flip <c>Installation.Status</c> itself and delegate the credential
    /// revoke here.
    /// </remarks>
    Task<int> RevokeActiveAsync(int installationId, DateTime revokedAt,
        CancellationToken ct = default);
}
