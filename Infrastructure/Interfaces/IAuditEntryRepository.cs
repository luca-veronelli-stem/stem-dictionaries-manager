using Core.Enums;
using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IAuditEntryRepository : IRepository<AuditEntryEntity>
{
    Task<IReadOnlyList<AuditEntryEntity>> GetByEntityAsync(AuditEntityType entityType, int entityId, 
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEntryEntity>> GetByUserAsync(int userId, 
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEntryEntity>> GetRecentAsync(int count, 
        CancellationToken cancellationToken = default);
}
