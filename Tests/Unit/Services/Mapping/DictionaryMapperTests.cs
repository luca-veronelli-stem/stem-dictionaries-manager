using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per DictionaryMapper (Domain v2).
/// IsStandard flag, nessun DeviceType/BoardType.
/// </summary>
public class DictionaryMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsDictionary()
    {
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "optimus-xp",
            Description = "Dizionario Optimus XP",
            IsStandard = false
        };

        var result = DictionaryMapper.ToDomain(entity);

        Assert.Equal(1, result.Id);
        Assert.Equal("optimus-xp", result.Name);
        Assert.Equal("Dizionario Optimus XP", result.Description);
        Assert.False(result.IsStandard);
    }

    [Fact]
    public void ToDomain_StandardDictionary_SetsIsStandard()
    {
        var entity = new DictionaryEntity
        {
            Id = 2,
            Name = "standard",
            Description = "Variabili standard condivise",
            IsStandard = true
        };

        var result = DictionaryMapper.ToDomain(entity);

        Assert.True(result.IsStandard);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DictionaryMapper.ToDomain(null!));
    }

    [Fact]
    public void ToDomainWithVariables_IncludesAllVariables()
    {
        var entity = new DictionaryEntity
        {
            Id = 1,
            Name = "dict-with-vars",
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

        var result = DictionaryMapper.ToDomainWithVariables(entity);

        Assert.Equal(3, result.Variables.Count);
    }

    [Fact]
    public void ToDomainWithVariables_EmptyVariables_ReturnsEmptyList()
    {
        var entity = new DictionaryEntity { Id = 1, Name = "empty-dict", Variables = [] };

        Assert.Empty(DictionaryMapper.ToDomainWithVariables(entity).Variables);
    }

    [Fact]
    public void ToDomainWithVariables_NullVariables_ReturnsEmptyList()
    {
        var entity = new DictionaryEntity { Id = 1, Name = "null-vars-dict", Variables = null! };

        Assert.Empty(DictionaryMapper.ToDomainWithVariables(entity).Variables);
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        var dictionary = Dictionary.Restore(10, "test-dictionary", "Test description", false, []);

        var result = DictionaryMapper.ToEntity(dictionary);

        Assert.Equal(10, result.Id);
        Assert.Equal("test-dictionary", result.Name);
        Assert.Equal("Test description", result.Description);
        Assert.False(result.IsStandard);
    }

    [Fact]
    public void ToEntity_StandardDictionary_SetsIsStandard()
    {
        var dictionary = Dictionary.Restore(1, "standard", "Standard variables", true, []);

        var result = DictionaryMapper.ToEntity(dictionary);

        Assert.True(result.IsStandard);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DictionaryMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        var entity = new DictionaryEntity
        {
            Id = 1, Name = "old-name", Description = "Old description", IsStandard = false
        };

        var domain = Dictionary.Restore(1, "new-name", "New description", true, []);

        DictionaryMapper.UpdateEntity(entity, domain);

        Assert.Equal("new-name", entity.Name);
        Assert.Equal("New description", entity.Description);
        Assert.True(entity.IsStandard);
        Assert.Equal(1, entity.Id);
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        var entities = new List<DictionaryEntity>
        {
            new() { Id = 1, Name = "dict1", Description = "D1", IsStandard = true },
            new() { Id = 2, Name = "dict2", Description = "D2", IsStandard = false },
            new() { Id = 3, Name = "dict3", Description = null }
        };

        var result = DictionaryMapper.ToDomainList(entities);

        Assert.Equal(3, result.Count);
        Assert.True(result[0].IsStandard);
        Assert.False(result[1].IsStandard);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        var originalEntity = new DictionaryEntity
        {
            Id = 42, Name = "roundtrip-dict", Description = "RoundTrip Test", IsStandard = true
        };

        var domain = DictionaryMapper.ToDomain(originalEntity);
        var resultEntity = DictionaryMapper.ToEntity(domain);

        Assert.Equal(originalEntity.Id, resultEntity.Id);
        Assert.Equal(originalEntity.Name, resultEntity.Name);
        Assert.Equal(originalEntity.Description, resultEntity.Description);
        Assert.Equal(originalEntity.IsStandard, resultEntity.IsStandard);
    }
}
