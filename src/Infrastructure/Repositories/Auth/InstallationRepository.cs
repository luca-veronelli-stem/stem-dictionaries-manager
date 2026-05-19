using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Auth;

/// <summary>
/// EF Core repository for <see cref="InstallationEntity"/>.
/// Add/Update only track changes; caller commits via the shared
/// <see cref="AppDbContext"/> for atomic multi-entity writes.
/// </summary>
public class InstallationRepository : IInstallationRepository
{
    private readonly AppDbContext _context;

    public InstallationRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<InstallationEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.Installations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<InstallationEntity?> FindByInstallGuidAsync(Guid installGuid,
        CancellationToken ct = default)
        => _context.Installations.FirstOrDefaultAsync(i => i.InstallGuid == installGuid, ct);

    public async Task<IReadOnlyList<InstallationEntity>> ListAsync(string? clientApp,
        InstallationStatus? status, CancellationToken ct = default)
    {
        IQueryable<InstallationEntity> query = _context.Installations;
        if (!string.IsNullOrWhiteSpace(clientApp))
        {
            query = query.Where(i => i.ClientApp == clientApp);
        }
        if (status is not null)
        {
            query = query.Where(i => i.Status == status);
        }
        return await query.OrderBy(i => i.Id).ToListAsync(ct);
    }

    public async Task<InstallationEntity> AddAsync(InstallationEntity entity,
        CancellationToken ct = default)
    {
        await _context.Installations.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(InstallationEntity entity, CancellationToken ct = default)
    {
        _context.Installations.Update(entity);
        return Task.CompletedTask;
    }
}
