namespace Services.Interfaces.Auth;

/// <summary>
/// Hot-path validator for per-installation API credentials.
/// Backed by an in-process cache (see research.md § R4) keyed on a SHA-256
/// digest of the plaintext to avoid retaining sensitive material in cache
/// memory beyond the immediate request scope.
/// </summary>
public interface IInstallationCredentialValidator
{
    /// <summary>
    /// Returns the active <c>Installation.Id</c> for <paramref name="plaintext"/>,
    /// or <c>null</c> if the plaintext does not match any active credential
    /// (unknown, revoked, or installation revoked).
    /// </summary>
    Task<int?> ValidateAsync(string plaintext, CancellationToken ct = default);

    /// <summary>
    /// Synchronously evicts the cached resolution for <paramref name="plaintext"/>.
    /// Called by the revoke flow after the DB write commits — guarantees the
    /// SC-004 ≤ 5 s revocation latency holds even within the cache TTL window.
    /// </summary>
    void Invalidate(string plaintext);
}
