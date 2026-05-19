using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Auth;

/// <summary>
/// EF Core repository for <see cref="InstallationApiCredentialEntity"/>.
/// Add/Update only track changes; caller commits via the shared
/// <see cref="AppDbContext"/> for atomic multi-entity writes.
/// </summary>
public class InstallationApiCredentialRepository : IInstallationApiCredentialRepository
{
    private readonly AppDbContext _context;

    public InstallationApiCredentialRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<InstallationApiCredentialEntity?> GetByInstallationIdAsync(int installationId,
        CancellationToken ct = default)
    {
        return _context.InstallationApiCredentials
            .FirstOrDefaultAsync(c => c.InstallationId == installationId, ct);
    }

    public async Task<IReadOnlyList<InstallationApiCredentialEntity>> ListAllActiveAsync(
        CancellationToken ct = default)
    {
        return await _context.InstallationApiCredentials
            .Where(c => c.Status == InstallationStatus.Active)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<InstallationApiCredentialEntity>>
        ListActiveByInstallationIdAsync(int installationId, CancellationToken ct = default)
    {
        return await _context.InstallationApiCredentials
            .Where(c => c.InstallationId == installationId
                && c.Status == InstallationStatus.Active)
            .ToListAsync(ct);
    }

    public async Task<InstallationApiCredentialEntity> AddAsync(
        InstallationApiCredentialEntity entity, CancellationToken ct = default)
    {
        await _context.InstallationApiCredentials.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(InstallationApiCredentialEntity entity, CancellationToken ct = default)
    {
        _context.InstallationApiCredentials.Update(entity);
        return Task.CompletedTask;
    }
}
