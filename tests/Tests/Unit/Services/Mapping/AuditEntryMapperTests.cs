using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per AuditEntryMapper.
/// </summary>
public class AuditEntryMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsAuditEntry()
    {
        // Arrange
        var changedAt = new DateTime(2026, 4, 8, 10, 0, 0, DateTimeKind.Utc);
        var entity = new AuditEntryEntity
        {
            Id = 1,
            EntityType = AuditEntityType.Variable,
            EntityId = 42,
            Operation = AuditOperation.Update,
            ChangedById = 5,
            ChangedAt = changedAt,
            PreviousValue = "{\"name\":\"old\"}",
            NewValue = "{\"name\":\"new\"}",
            Notes = "Aggiornamento nome"
        };

        // Act
        var result = AuditEntryMapper.ToDomain(entity);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(AuditEntityType.Variable, result.EntityType);
        Assert.Equal(42, result.EntityId);
        Assert.Equal(AuditOperation.Update, result.Operation);
        Assert.Equal(5, result.ChangedById);
        Assert.Equal(changedAt, result.ChangedAt);
        Assert.Equal("{\"name\":\"old\"}", result.PreviousValue);
        Assert.Equal("{\"name\":\"new\"}", result.NewValue);
        Assert.Equal("Aggiornamento nome", result.Notes);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => AuditEntryMapper.ToDomain(null!));
    }

    [Fact]
    public void ToDomain_NullableFieldsAsNull_MapsCorrectly()
    {
        // Arrange
        var entity = new AuditEntryEntity
        {
            Id = 2,
            EntityType = AuditEntityType.Command,
            EntityId = 10,
            Operation = AuditOperation.Create,
            ChangedById = 1,
            ChangedAt = DateTime.UtcNow,
            PreviousValue = null,
            NewValue = "{\"name\":\"cmd\"}",
            Notes = null
        };

        // Act
        var result = AuditEntryMapper.ToDomain(entity);

        // Assert
        Assert.Null(result.PreviousValue);
        Assert.NotNull(result.NewValue);
        Assert.Null(result.Notes);
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        // Arrange
        var changedAt = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        var domain = AuditEntry.Restore(
            id: 3,
            entityType: AuditEntityType.Dictionary,
            entityId: 7,
            operation: AuditOperation.Delete,
            changedById: 2,
            changedAt: changedAt,
            previousValue: "{\"name\":\"old_dict\"}",
            newValue: null,
            notes: "Rimosso dizionario");

        // Act
        var result = AuditEntryMapper.ToEntity(domain);

        // Assert
        Assert.Equal(AuditEntityType.Dictionary, result.EntityType);
        Assert.Equal(7, result.EntityId);
        Assert.Equal(AuditOperation.Delete, result.Operation);
        Assert.Equal(2, result.ChangedById);
        Assert.Equal(changedAt, result.ChangedAt);
        Assert.Equal("{\"name\":\"old_dict\"}", result.PreviousValue);
        Assert.Null(result.NewValue);
        Assert.Equal("Rimosso dizionario", result.Notes);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => AuditEntryMapper.ToEntity(null!));
    }

    [Fact]
    public void ToEntity_DoesNotMapId()
    {
        // Arrange — Id viene assegnato dal DB, non dal mapper
        var domain = AuditEntry.Restore(
            id: 99,
            entityType: AuditEntityType.Board,
            entityId: 1,
            operation: AuditOperation.Create,
            changedById: 1,
            changedAt: DateTime.UtcNow,
            previousValue: null,
            newValue: "{}",
            notes: null);

        // Act
        var result = AuditEntryMapper.ToEntity(domain);

        // Assert — Id non mappato (default 0), sarà assegnato da EF
        Assert.Equal(0, result.Id);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        // Arrange
        var changedAt = new DateTime(2026, 3, 15, 8, 30, 0, DateTimeKind.Utc);
        var original = new AuditEntryEntity
        {
            Id = 10,
            EntityType = AuditEntityType.User,
            EntityId = 3,
            Operation = AuditOperation.Update,
            ChangedById = 1,
            ChangedAt = changedAt,
            PreviousValue = "{\"displayName\":\"Old\"}",
            NewValue = "{\"displayName\":\"New\"}",
            Notes = "Cambio nome"
        };

        // Act
        var domain = AuditEntryMapper.ToDomain(original);
        var roundTripped = AuditEntryMapper.ToEntity(domain);

        // Assert — tutti i campi preservati tranne Id
        Assert.Equal(original.EntityType, roundTripped.EntityType);
        Assert.Equal(original.EntityId, roundTripped.EntityId);
        Assert.Equal(original.Operation, roundTripped.Operation);
        Assert.Equal(original.ChangedById, roundTripped.ChangedById);
        Assert.Equal(original.ChangedAt, roundTripped.ChangedAt);
        Assert.Equal(original.PreviousValue, roundTripped.PreviousValue);
        Assert.Equal(original.NewValue, roundTripped.NewValue);
        Assert.Equal(original.Notes, roundTripped.Notes);
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        // Arrange
        var entities = new List<AuditEntryEntity>
        {
            new()
            {
                Id = 1, EntityType = AuditEntityType.Variable, EntityId = 1,
                Operation = AuditOperation.Create, ChangedById = 1,
                ChangedAt = DateTime.UtcNow, NewValue = "{}"
            },
            new()
            {
                Id = 2, EntityType = AuditEntityType.Command, EntityId = 2,
                Operation = AuditOperation.Delete, ChangedById = 2,
                ChangedAt = DateTime.UtcNow, PreviousValue = "{}"
            }
        };

        // Act
        var result = AuditEntryMapper.ToDomainList(entities);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(AuditEntityType.Variable, result[0].EntityType);
        Assert.Equal(AuditEntityType.Command, result[1].EntityType);
    }

    [Fact]
    public void ToDomainList_EmptyList_ReturnsEmpty()
    {
        var result = AuditEntryMapper.ToDomainList([]);
        Assert.Empty(result);
    }

    [Fact]
    public void ToDomainList_NullList_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => AuditEntryMapper.ToDomainList(null!));
    }
}
