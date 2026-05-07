using Core.Models.Auth;

namespace Services.Interfaces.Auth;

/// <summary>
/// Bootstrap-token domain operations: admin mint (US2) plus the
/// registration-flow lookup + mark-used path (US1).
/// </summary>
public interface IBootstrapTokenService
{
    /// <summary>
    /// Mints a fresh single-use bootstrap token scoped to
    /// <paramref name="clientApp"/>. The plaintext is generated server-side
    /// via <c>ITokenGenerator</c>, hashed via <c>IPasswordHasher</c>, and
    /// returned to the caller exactly once — only the hash is persisted
    /// (FR-014, data-model invariant 4 — plaintext-once). When
    /// <paramref name="ttl"/> is <c>null</c>, defaults to 30 days; otherwise
    /// it must fall within [1h, 90d] per FR-007.
    /// </summary>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="clientApp"/> is null or whitespace.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ttl"/> falls outside [1h, 90d].
    /// </exception>
    Task<(BootstrapToken Record, string Plaintext)> MintAsync(string clientApp,
        TimeSpan? ttl, CancellationToken ct = default);

    /// <summary>
    /// PBKDF2-verifies <paramref name="plaintext"/> against every active
    /// (<see cref="Core.Enums.Auth.BootstrapTokenStatus.Issued"/>) token row,
    /// returning the matched domain model or <c>null</c> on miss. Iteration
    /// is bounded to the active set per <c>tasks.md</c> T045 to avoid an
    /// unbounded PBKDF2 cost as terminal-state rows accumulate.
    /// </summary>
    Task<BootstrapToken?> LookupAsync(string plaintext, CancellationToken ct = default);

    /// <summary>
    /// Transitions the persisted token row to
    /// <see cref="Core.Enums.Auth.BootstrapTokenStatus.Used"/>, recording the
    /// consuming installation id. Does <b>not</b> commit — the caller's
    /// shared <c>AppDbContext</c> is the unit of work, so the state change
    /// stays atomic with the installation/credential/audit writes per
    /// <c>data-model.md</c> invariant 3.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the token is not in
    /// <see cref="Core.Enums.Auth.BootstrapTokenStatus.Issued"/> state, or
    /// when <paramref name="tokenId"/> does not resolve to a row.
    /// </exception>
    Task MarkUsedAsync(int tokenId, int installationId, DateTime usedAt,
        CancellationToken ct = default);
}
