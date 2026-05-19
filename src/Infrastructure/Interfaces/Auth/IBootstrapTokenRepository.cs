using Infrastructure.Entities.Auth;

namespace Infrastructure.Interfaces.Auth;

/// <summary>
/// Repository surface for <see cref="BootstrapTokenEntity"/>.
/// No <c>GetBySecretHashAsync</c>: lookup-by-plaintext requires PBKDF2-verifying
/// the candidate against every row, which lives in
/// <c>BootstrapTokenService.LookupAsync</c>. The lookup intentionally iterates
/// across all statuses so the caller can branch on <c>Status</c> for the
/// non-race Used / Revoked outcomes (#58); narrowing earlier conflates them
/// with token-unknown into a single 401 path.
/// </summary>
public interface IBootstrapTokenRepository
{
    Task<BootstrapTokenEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<BootstrapTokenEntity>> ListAllAsync(CancellationToken ct = default);
    Task<BootstrapTokenEntity> AddAsync(BootstrapTokenEntity entity, CancellationToken ct = default);
    Task UpdateAsync(BootstrapTokenEntity entity, CancellationToken ct = default);
}
