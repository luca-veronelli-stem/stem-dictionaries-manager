using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Auth;

/// <summary>
/// EF Core repository for <see cref="BootstrapTokenEntity"/>.
/// </summary>
/// <remarks>
/// Add/Update only track changes — they do NOT call <c>SaveChangesAsync</c>.
/// The caller (typically <c>RegistrationService</c> or
/// <c>BootstrapTokenService</c>) is responsible for committing via the
/// shared <see cref="AppDbContext"/>, so multi-entity writes can be
/// atomic per <c>data-model.md</c> invariant 3.
/// </remarks>
public class BootstrapTokenRepository : IBootstrapTokenRepository
{
    private readonly AppDbContext _context;

    public BootstrapTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<BootstrapTokenEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.BootstrapTokens.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<BootstrapTokenEntity>> ListByStatusAsync(
        BootstrapTokenStatus status, CancellationToken ct = default)
    {
        return await _context.BootstrapTokens
            .Where(t => t.Status == status)
            .ToListAsync(ct);
    }

    public async Task<BootstrapTokenEntity> AddAsync(BootstrapTokenEntity entity,
        CancellationToken ct = default)
    {
        await _context.BootstrapTokens.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(BootstrapTokenEntity entity, CancellationToken ct = default)
    {
        _context.BootstrapTokens.Update(entity);
        return Task.CompletedTask;
    }
}
