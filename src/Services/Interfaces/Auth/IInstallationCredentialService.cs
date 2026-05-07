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
}
