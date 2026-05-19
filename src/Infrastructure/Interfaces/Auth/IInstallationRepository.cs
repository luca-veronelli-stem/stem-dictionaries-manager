using Core.Enums.Auth;
using Infrastructure.Entities.Auth;

namespace Infrastructure.Interfaces.Auth;

public interface IInstallationRepository
{
    Task<InstallationEntity?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Resolves the single Installation row carrying the supplied
    /// <paramref name="installGuid"/>. The unique index on
    /// <c>Installations.InstallGuid</c> guarantees zero or one match.
    /// Returns <c>null</c> when no row exists.
    /// </summary>
    Task<InstallationEntity?> FindByInstallGuidAsync(Guid installGuid,
        CancellationToken ct = default);

    Task<IReadOnlyList<InstallationEntity>> ListAsync(string? clientApp,
        InstallationStatus? status, CancellationToken ct = default);
    Task<InstallationEntity> AddAsync(InstallationEntity entity, CancellationToken ct = default);
    Task UpdateAsync(InstallationEntity entity, CancellationToken ct = default);
}
