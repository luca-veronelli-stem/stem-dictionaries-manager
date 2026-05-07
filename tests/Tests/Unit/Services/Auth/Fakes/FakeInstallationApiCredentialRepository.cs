using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;

namespace Tests.Unit.Services.Auth.Fakes;

/// <summary>
/// Manual fake for <see cref="IInstallationApiCredentialRepository"/>. Records
/// the number of <see cref="ListAllActiveAsync"/> calls so tests can assert
/// the validator's cache-hit short-circuit holds.
/// </summary>
internal sealed class FakeInstallationApiCredentialRepository : IInstallationApiCredentialRepository
{
    private readonly List<InstallationApiCredentialEntity> _store = [];

    public int ListAllActiveAsyncCallCount { get; private set; }

    public void Seed(InstallationApiCredentialEntity entity) => _store.Add(entity);

    public Task<IReadOnlyList<InstallationApiCredentialEntity>> ListAllActiveAsync(
        CancellationToken ct = default)
    {
        ListAllActiveAsyncCallCount++;
        IReadOnlyList<InstallationApiCredentialEntity> active = _store
            .Where(e => e.Status == InstallationStatus.Active)
            .ToList();
        return Task.FromResult(active);
    }

    public Task<InstallationApiCredentialEntity?> GetByInstallationIdAsync(int installationId,
        CancellationToken ct = default)
    {
        InstallationApiCredentialEntity? hit = _store
            .FirstOrDefault(e => e.InstallationId == installationId);
        return Task.FromResult(hit);
    }

    public Task<InstallationApiCredentialEntity> AddAsync(InstallationApiCredentialEntity entity,
        CancellationToken ct = default)
    {
        _store.Add(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(InstallationApiCredentialEntity entity, CancellationToken ct = default)
        => Task.CompletedTask;
}
