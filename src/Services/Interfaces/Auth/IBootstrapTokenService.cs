using Core.Models.Auth;

namespace Services.Interfaces.Auth;

/// <summary>
/// Bootstrap-token domain operations for the registration flow.
/// Mint comes in US2; this surface covers US1's lookup + mark-used path.
/// </summary>
public interface IBootstrapTokenService
{
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
