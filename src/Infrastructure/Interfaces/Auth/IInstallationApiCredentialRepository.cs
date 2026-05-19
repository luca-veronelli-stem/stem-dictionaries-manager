using Core.Enums.Auth;
using Infrastructure.Entities.Auth;

namespace Infrastructure.Interfaces.Auth;

public interface IInstallationApiCredentialRepository
{
    Task<InstallationApiCredentialEntity?> GetByInstallationIdAsync(int installationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<InstallationApiCredentialEntity>> ListAllActiveAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Returns every credential row for the supplied
    /// <paramref name="installationId"/> whose
    /// <see cref="InstallationApiCredentialEntity.Status"/> equals
    /// <see cref="InstallationStatus.Active"/>. Used by the
    /// re-registration flow to flip the prior-active row(s) before
    /// issuing a fresh credential. The filtered unique index on
    /// <c>InstallationId WHERE Status = Active</c> means the list is
    /// either empty or single-element in practice.
    /// </summary>
    Task<IReadOnlyList<InstallationApiCredentialEntity>> ListActiveByInstallationIdAsync(
        int installationId, CancellationToken ct = default);

    Task<InstallationApiCredentialEntity> AddAsync(InstallationApiCredentialEntity entity,
        CancellationToken ct = default);
    Task UpdateAsync(InstallationApiCredentialEntity entity, CancellationToken ct = default);
}
