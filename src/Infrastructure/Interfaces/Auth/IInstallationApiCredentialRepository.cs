using Infrastructure.Entities.Auth;

namespace Infrastructure.Interfaces.Auth;

public interface IInstallationApiCredentialRepository
{
    Task<InstallationApiCredentialEntity?> GetByInstallationIdAsync(int installationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<InstallationApiCredentialEntity>> ListAllActiveAsync(
        CancellationToken ct = default);
    Task<InstallationApiCredentialEntity> AddAsync(InstallationApiCredentialEntity entity,
        CancellationToken ct = default);
    Task UpdateAsync(InstallationApiCredentialEntity entity, CancellationToken ct = default);
}
