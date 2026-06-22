using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class AuditEntryRepository : RepositoryBase<AuditEntryEntity>, IAuditEntryRepository
{
    public AuditEntryRepository(AppDbContext context, ILogger<RepositoryBase<AuditEntryEntity>> logger)
        : base(context, logger)
    {
    }

    public override async Task<AuditEntryEntity?> GetByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.ChangedBy)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryEntity>> GetByEntityAsync(AuditEntityType entityType,
        int entityId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.ChangedBy)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryEntity>> GetByUserAsync(int userId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.ChangedBy)
            .Where(a => a.ChangedById == userId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryEntity>> GetRecentAsync(int count,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.ChangedBy)
            .OrderByDescending(a => a.ChangedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEntryEntity>> GetByDateRangeAsync(DateTime from, DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(a => a.ChangedBy)
            .Where(a => a.ChangedAt >= from && a.ChangedAt <= to)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    // AuditEntry must not be updated or deleted
    public override Task UpdateAsync(AuditEntryEntity entity, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("AuditEntry cannot be updated.");
    }

    public override Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("AuditEntry cannot be deleted.");
    }
}
