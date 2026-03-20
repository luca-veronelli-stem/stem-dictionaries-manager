using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per DictionaryMapper.
/// Verifica il mapping dell'aggregate root con variables.
/// </summary>
public class DictionaryMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsDictionary()
    {
        // Arrange
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "optimus-xp",
            Description = "Dizionario Optimus XP",
            BoardTypeId = 5,
            DeviceType = DeviceType.Optimus,
            BoardType = new BoardTypeEntity { Id = 5, Name = "Madre", FirmwareType = 17 }
        };

        // Act
        var result = DictionaryMapper.ToDomain(entity);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("optimus-xp", result.Name);
        Assert.Equal("Dizionario Optimus XP", result.Description);
        Assert.Equal(DeviceType.Optimus, result.DeviceType);
        Assert.NotNull(result.BoardType);
        Assert.Equal("Madre", result.BoardType!.Name);
    }

    [Fact]
    public void ToDomain_StandardDictionary_HasNullBoardType()
    {
        // Arrange - Dizionario "Standard" senza BoardType
        var entity = new DictionaryEntity
        {
            Id = 2,
            Name = "standard",
            Description = "Variabili standard condivise",
            BoardTypeId = null,
            BoardType = null
        };

        // Act
        var result = DictionaryMapper.ToDomain(entity);

        // Assert
        Assert.Equal("standard", result.Name);
        Assert.Null(result.DeviceType);
        Assert.Null(result.BoardType);
    }

    [Fact]
    public void ToDomain_WithExplicitBoardType_UsesBoardType()
    {
        // Arrange
        var entity = new DictionaryEntity
        {
            Id = 3,
            Name = "test-dict",
            Description = null,
            DeviceType = DeviceType.Eden,
            BoardTypeId = 10,
            BoardType = null // Non caricato
        };
        var boardType = BoardType.Restore(10, "CustomBoard", 99);

        // Act
        var result = DictionaryMapper.ToDomain(entity, boardType);

        // Assert
        Assert.NotNull(result.BoardType);
        Assert.Equal("CustomBoard", result.BoardType!.Name);
        Assert.Equal(99, result.BoardType.FirmwareType);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DictionaryMapper.ToDomain(null!));
    }

    [Fact]
    public void ToDomainWithVariables_IncludesAllVariables()
    {
        // Arrange
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "dict-with-vars",
            Description = null,
            BoardTypeId = null,
            Variables =
            [
                new() { Id = 1, Name = "Var1", AddressHigh = 0x00, AddressLow = 0x01,
                        DataTypeKind = DataTypeKind.UInt8, DataTypeRaw = "uint8_t",
                        AccessMode = AccessMode.ReadOnly, IsEnabled = true },
                new() { Id = 2, Name = "Var2", AddressHigh = 0x00, AddressLow = 0x02,
                        DataTypeKind = DataTypeKind.UInt16, DataTypeRaw = "uint16_t",
                        AccessMode = AccessMode.ReadWrite, IsEnabled = true },
                new() { Id = 3, Name = "Var3", AddressHigh = 0x00, AddressLow = 0x03,
                        DataTypeKind = DataTypeKind.Float, DataTypeRaw = "float",
                        AccessMode = AccessMode.ReadOnly, IsEnabled = false }
            ]
        };

        // Act
        var result = DictionaryMapper.ToDomainWithVariables(entity);

        // Assert
        Assert.Equal(3, result.Variables.Count);
        Assert.Equal("Var1", result.Variables[0].Name);
        Assert.Equal("Var2", result.Variables[1].Name);
        Assert.Equal("Var3", result.Variables[2].Name);
    }

    [Fact]
    public void ToDomainWithVariables_EmptyVariables_ReturnsEmptyList()
    {
        // Arrange
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "empty-dict",
            Description = null,
            BoardTypeId = null,
            Variables = []
        };

        // Act
        var result = DictionaryMapper.ToDomainWithVariables(entity);

        // Assert
        Assert.Empty(result.Variables);
    }

    [Fact]
    public void ToDomainWithVariables_NullVariables_ReturnsEmptyList()
    {
        // Arrange
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "null-vars-dict",
            Description = null,
            BoardTypeId = null,
            Variables = null!
        };

        // Act
        var result = DictionaryMapper.ToDomainWithVariables(entity);

        // Assert
        Assert.Empty(result.Variables);
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        // Arrange
        var boardType = BoardType.Restore(5, "TestBoard", 10);
        var dictionary = Dictionary.Restore(
            id: 10,
            name: "test-dictionary",
            deviceType: DeviceType.Optimus,
            boardType: boardType,
            description: "Test description",
            variables: []);

        // Act
        var result = DictionaryMapper.ToEntity(dictionary);

        // Assert
        Assert.Equal(10, result.Id);
        Assert.Equal("test-dictionary", result.Name);
        Assert.Equal("Test description", result.Description);
        Assert.Equal(DeviceType.Optimus, result.DeviceType);
        Assert.Equal(5, result.BoardTypeId);
    }

    [Fact]
    public void ToEntity_StandardDictionary_HasNullBoardTypeId()
    {
        // Arrange
        var dictionary = Dictionary.Restore(
            id: 1,
            name: "standard",
            deviceType: null,
            boardType: null,
            description: "Standard variables",
            variables: []);

        // Act
        var result = DictionaryMapper.ToEntity(dictionary);

        // Assert
        Assert.Null(result.BoardTypeId);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DictionaryMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        // Arrange
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "old-name",
            Description = "Old description",
            BoardTypeId = 5
        };

        var newBoardType = BoardType.Restore(10, "NewBoard", 20);
        var domain = Dictionary.Restore(1, "new-name", DeviceType.Eden, newBoardType, "New description", []);

        // Act
        DictionaryMapper.UpdateEntity(entity, domain);

        // Assert
        Assert.Equal("new-name", entity.Name);
        Assert.Equal("New description", entity.Description);
        Assert.Equal(10, entity.BoardTypeId);
        Assert.Equal(1, entity.Id); // Id non cambia
    }

    [Fact]
    public void UpdateEntity_SetBoardTypeToNull_ClearsBoardTypeId()
    {
        // Arrange
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "test",
            Description = null,
            BoardTypeId = 5
        };

        var domain = Dictionary.Restore(1, "standard", null, null, null, []);

        // Act
        DictionaryMapper.UpdateEntity(entity, domain);

        // Assert
        Assert.Null(entity.BoardTypeId);
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        // Arrange
        var entities = new List<DictionaryEntity>
        {
            new() { Id = 1, Name = "dict1", Description = "D1", DeviceType = null, BoardTypeId = null },
            new() { Id = 2, Name = "dict2", Description = "D2", DeviceType = DeviceType.Optimus, BoardTypeId = 5,
                    BoardType = new BoardTypeEntity { Id = 5, Name = "Board5", FirmwareType = 5 } },
            new() { Id = 3, Name = "dict3", Description = null, DeviceType = null, BoardTypeId = null }
        };

        // Act
        var result = DictionaryMapper.ToDomainList(entities);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("dict1", result[0].Name);
        Assert.Null(result[0].BoardType);
        Assert.Equal("dict2", result[1].Name);
        Assert.NotNull(result[1].BoardType);
        Assert.Equal("dict3", result[2].Name);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        // Arrange
        var originalEntity = new DictionaryEntity
        {
            Id = 42,
            Name = "roundtrip-dict",
            Description = "RoundTrip Test",
            BoardTypeId = 99,
            DeviceType = DeviceType.Optimus,
            BoardType = new BoardTypeEntity { Id = 99, Name = "RoundTripBoard", FirmwareType = 88 }
        };

        // Act
        var domain = DictionaryMapper.ToDomain(originalEntity);
        var resultEntity = DictionaryMapper.ToEntity(domain);

        // Assert
        Assert.Equal(originalEntity.Id, resultEntity.Id);
        Assert.Equal(originalEntity.Name, resultEntity.Name);
        Assert.Equal(originalEntity.Description, resultEntity.Description);
        Assert.Equal(originalEntity.BoardTypeId, resultEntity.BoardTypeId);
    }
}
