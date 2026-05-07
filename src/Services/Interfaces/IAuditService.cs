using Core.Enums;
using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Audit trail service.
/// Query: read change history.
/// Log: record changes (called by other services).
/// </summary>
public interface IAuditService
{
    // === Query ===

    /// <summary>
    /// Gets an audit entry by ID.
    /// </summary>
    Task<AuditEntry?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets audit entries for a specific entity.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        AuditEntityType entityType, int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets audit entries for a user.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByUserAsync(
        int userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent audit entries.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetRecentAsync(
        int count = 100, CancellationToken ct = default);

    /// <summary>
    /// Gets audit entries in a date range (UTC).
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default);

    // === Log ===

    /// <summary>
    /// Records a create operation.
    /// </summary>
    Task LogCreateAsync(AuditEntityType entityType, int entityId,
        int changedById, string newValueJson, string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Records an update operation.
    /// </summary>
    Task LogUpdateAsync(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string newValueJson,
        string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Records a delete operation.
    /// </summary>
    Task LogDeleteAsync(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string? notes = null,
        CancellationToken ct = default);
}
