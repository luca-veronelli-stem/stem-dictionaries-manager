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
    /// Reachable only when the caller already has the plaintext in hand
    /// (e.g. fresh from <c>POST /register</c>).
    /// </summary>
    void Invalidate(string plaintext);

    /// <summary>
    /// Synchronously evicts the cached resolution that maps to
    /// <paramref name="installationId"/>. Called by the admin revoke flow
    /// after the DB write commits; the admin never holds the plaintext
    /// (FR-014 plaintext-once), so the lookup goes installation-side via
    /// a side-index maintained on positive resolutions. Guarantees the
    /// SC-004 revocation latency stays well under the 5 s TTL ceiling.
    /// </summary>
    void Invalidate(int installationId);
}
