using Core.Enums.Auth;
using Infrastructure.Entities.Auth;

namespace Infrastructure.Interfaces.Auth;

/// <summary>
/// Repository surface for <see cref="BootstrapTokenEntity"/>.
/// No <c>GetBySecretHashAsync</c>: lookup-by-plaintext requires PBKDF2-verifying
/// the candidate against every active row, which lives in
/// <c>BootstrapTokenService.LookupAsync</c>.
/// </summary>
public interface IBootstrapTokenRepository
{
    Task<BootstrapTokenEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<BootstrapTokenEntity>> ListByStatusAsync(
        BootstrapTokenStatus status, CancellationToken ct = default);
    Task<BootstrapTokenEntity> AddAsync(BootstrapTokenEntity entity, CancellationToken ct = default);
    Task UpdateAsync(BootstrapTokenEntity entity, CancellationToken ct = default);
}
