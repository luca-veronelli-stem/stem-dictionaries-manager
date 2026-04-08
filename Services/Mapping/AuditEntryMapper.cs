using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per AuditEntry Entity ↔ Domain.
/// Nota: non serve UpdateEntity (AuditEntry è immutabile).
/// </summary>
public static class AuditEntryMapper
{
    /// <summary>
    /// Converte AuditEntryEntity in AuditEntry (Domain).
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
    /// Converte AuditEntry (Domain) in AuditEntryEntity per creazione.
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
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<AuditEntry> ToDomainList(
        IReadOnlyList<AuditEntryEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities.Select(ToDomain).ToList();
    }
}
