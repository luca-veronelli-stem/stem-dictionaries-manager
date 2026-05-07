using Core.Enums.Auth;
using Infrastructure.Entities.Auth;

namespace Infrastructure.Interfaces.Auth;

public interface IInstallationRepository
{
    Task<InstallationEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<InstallationEntity>> ListAsync(string? clientApp,
        InstallationStatus? status, CancellationToken ct = default);
    Task<InstallationEntity> AddAsync(InstallationEntity entity, CancellationToken ct = default);
    Task UpdateAsync(InstallationEntity entity, CancellationToken ct = default);
}
