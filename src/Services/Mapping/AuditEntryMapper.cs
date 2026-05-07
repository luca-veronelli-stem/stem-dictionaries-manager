using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for AuditEntry Entity ↔ Domain.
/// Note: no UpdateEntity needed (AuditEntry is immutable).
/// </summary>
public static class AuditEntryMapper
{
    /// <summary>
    /// Converts AuditEntryEntity to AuditEntry (Domain).
    /// </summary>
    public static AuditEntry ToDomain(AuditEntryEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return AuditEntry.Restore(
            entity.Id,
            entity.EntityType,
            entity.EntityId,
            entity.Operation,
            entity.ChangedById,
            entity.ChangedAt,
            entity.PreviousValue,
            entity.NewValue,
            entity.Notes);
    }

    /// <summary>
    /// Converts AuditEntry (Domain) to AuditEntryEntity for creation.
    /// </summary>
    public static AuditEntryEntity ToEntity(AuditEntry domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new AuditEntryEntity
        {
            EntityType = domain.EntityType,
            EntityId = domain.EntityId,
            Operation = domain.Operation,
            ChangedById = domain.ChangedById,
            ChangedAt = domain.ChangedAt,
            PreviousValue = domain.PreviousValue,
            NewValue = domain.NewValue,
            Notes = domain.Notes
        };
    }

    /// <summary>
    /// Converts a list of entities to a list of domain models.
    /// </summary>
    public static IReadOnlyList<AuditEntry> ToDomainList(
        IReadOnlyList<AuditEntryEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities.Select(ToDomain).ToList();
    }
}
