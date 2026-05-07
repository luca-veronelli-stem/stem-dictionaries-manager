using Core.Enums;

namespace Core.Models;

/// <summary>
/// Audit trail entry. Tracks every modification with the full JSON payload.
/// </summary>
public class AuditEntry
{
    public int Id { get; private set; }
    public AuditEntityType EntityType { get; private set; }
    public int EntityId { get; private set; }
    public AuditOperation Operation { get; private set; }
    public int ChangedById { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Notes { get; private set; }

    public AuditEntry(
        AuditEntityType entityType,
        int entityId,
        AuditOperation operation,
        int changedById,
        DateTime? changedAt = null,
        string? previousValue = null,
        string? newValue = null,
        string? notes = null)
    {
        EntityType = entityType;
        EntityId = entityId;
        Operation = operation;
        ChangedById = changedById;
        ChangedAt = changedAt ?? DateTime.UtcNow;
        PreviousValue = previousValue;
        NewValue = newValue;
        Notes = notes;
    }

    /// <summary>
    /// Factory method to reconstruct from the DB.
    /// </summary>
    public static AuditEntry Restore(
        int id,
        AuditEntityType entityType,
        int entityId,
        AuditOperation operation,
        int changedById,
        DateTime changedAt,
        string? previousValue,
        string? newValue,
        string? notes)
    {
        var entry = new AuditEntry(entityType, entityId, operation, changedById,
            changedAt, previousValue, newValue, notes)
        {
            Id = id
        };
        return entry;
    }

    /// <summary>
    /// Creates an entry for a CREATE operation.
    /// </summary>
    public static AuditEntry ForCreate(AuditEntityType entityType, int entityId,
        int changedById, string newValueJson, string? notes = null)
    {
        return new AuditEntry(entityType, entityId, AuditOperation.Create,
            changedById, null, null, newValueJson, notes);
    }

    /// <summary>
    /// Creates an entry for an UPDATE operation.
    /// </summary>
    public static AuditEntry ForUpdate(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string newValueJson, string? notes = null)
    {
        return new AuditEntry(entityType, entityId, AuditOperation.Update,
            changedById, null, previousValueJson, newValueJson, notes);
    }

    /// <summary>
    /// Creates an entry for a DELETE operation.
    /// </summary>
    public static AuditEntry ForDelete(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string? notes = null)
    {
        return new AuditEntry(entityType, entityId, AuditOperation.Delete,
            changedById, null, previousValueJson, null, notes);
    }
}
