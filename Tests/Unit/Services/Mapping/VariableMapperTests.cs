using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per VariableMapper.
/// Verifica il mapping complesso con DataTypeKind, DataTypeParam, DataTypeRaw.
/// </summary>
public class VariableMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsVariable()
    {
        // Arrange
        var entity = new VariableEntity
        {
            Id = 1,
            DictionaryId = 10,
            Name = "Firmware macchina",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeParam = null,
            DataTypeRaw = "uint16_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            MinValue = 0,
            MaxValue = 65535,
            Unit = null,
            Usage = "Versione firmware",
            Description = "Identificativo versione"
        };

        // Act
        var result = VariableMapper.ToDomain(entity);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Firmware macchina", result.Name);
        Assert.Equal(0x00, result.AddressHigh);
        Assert.Equal(0x01, result.AddressLow);
        Assert.Equal(DataTypeKind.UInt16, result.DataTypeKind);
        Assert.Null(result.DataTypeParam);
        Assert.Equal("uint16_t", result.DataTypeRaw);
        Assert.Equal(AccessMode.ReadOnly, result.AccessMode);
        Assert.True(result.IsEnabled);
        Assert.Equal(0, result.MinValue);
        Assert.Equal(65535, result.MaxValue);
        Assert.Equal("Versione firmware", result.Usage);
    }

    [Fact]
    public void ToDomain_EntityWithDataTypeParam_PreservesParam()
    {
        // Arrange - String[20]
        var entity = new VariableEntity
        {
            Id = 2,
            DictionaryId = 10,
            Name = "SerialNumber",
            AddressHigh = 0x00,
            AddressLow = 0x10,
            DataTypeKind = DataTypeKind.String,
            DataTypeParam = 20,
            DataTypeRaw = "String[20]",
            AccessMode = AccessMode.ReadWrite,
            IsEnabled = true
        };

        // Act
        var result = VariableMapper.ToDomain(entity);

        // Assert
        Assert.Equal(DataTypeKind.String, result.DataTypeKind);
        Assert.Equal(20, result.DataTypeParam);
        Assert.Equal("String[20]", result.DataTypeRaw);
    }

    [Fact]
    public void ToDomain_DeviceSpecificVariable_HasCorrectCategory()
    {
        // Arrange - AddressHigh = 0x80 = DeviceSpecific
        var entity = new VariableEntity
        {
            Id = 3,
            DictionaryId = 10,
            Name = "DeviceVar",
            AddressHigh = 0x80,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };

        // Act
        var result = VariableMapper.ToDomain(entity);

        // Assert
        Assert.Equal(VariableCategory.DeviceSpecific, result.Category);
        Assert.Equal(0x8001, result.FullAddress);
    }

    [Fact]
    public void ToDomain_StandardVariable_HasCorrectCategory()
    {
        // Arrange - AddressHigh = 0x00 = Standard
        var entity = new VariableEntity
        {
            Id = 4,
            DictionaryId = 10,
            Name = "StdVar",
            AddressHigh = 0x00,
            AddressLow = 0x05,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };

        // Act
        var result = VariableMapper.ToDomain(entity);

        // Assert
        Assert.Equal(VariableCategory.Standard, result.Category);
        Assert.Equal(0x0005, result.FullAddress);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VariableMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        // Arrange
        var variable = Variable.Restore(
            id: 5,
            name: "TestVar",
            addressHigh: 0x00,
            addressLow: 0x20,
            dataTypeKind: DataTypeKind.Float,
            dataTypeRaw: "float",
            dataTypeParam: null,
            accessMode: AccessMode.ReadWrite,
            isEnabled: true,
            format: null,
            minValue: -100.0,
            maxValue: 100.0,
            unit: "°C",
            usage: "Temperature",
            description: "Current temperature");

        // Act
        var result = VariableMapper.ToEntity(variable, dictionaryId: 99);

        // Assert
        Assert.Equal(5, result.Id);
        Assert.Equal(99, result.DictionaryId);
        Assert.Equal("TestVar", result.Name);
        Assert.Equal(0x00, result.AddressHigh);
        Assert.Equal(0x20, result.AddressLow);
        Assert.Equal(DataTypeKind.Float, result.DataTypeKind);
        Assert.Equal("float", result.DataTypeRaw);
        Assert.Equal(AccessMode.ReadWrite, result.AccessMode);
        Assert.Equal(-100.0, result.MinValue);
        Assert.Equal(100.0, result.MaxValue);
        Assert.Equal("°C", result.Unit);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => VariableMapper.ToEntity(null!, 1));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        // Arrange
        var entity = new VariableEntity
        {
            Id = 1,
            DictionaryId = 10,
            Name = "OldName",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        
        var domain = Variable.Restore(
            id: 1,
            name: "NewName",
            addressHigh: 0x80,
            addressLow: 0x02,
            dataTypeKind: DataTypeKind.UInt32,
            dataTypeRaw: "uint32_t",
            dataTypeParam: null,
            accessMode: AccessMode.ReadWrite,
            isEnabled: false,
            format: null,
            minValue: 0,
            maxValue: 1000,
            unit: "ms",
            usage: "Timeout",
            description: "Operation timeout");

        // Act
        VariableMapper.UpdateEntity(entity, domain);

        // Assert
        Assert.Equal("NewName", entity.Name);
        Assert.Equal(0x80, entity.AddressHigh);
        Assert.Equal(0x02, entity.AddressLow);
        Assert.Equal(DataTypeKind.UInt32, entity.DataTypeKind);
        Assert.Equal("uint32_t", entity.DataTypeRaw);
        Assert.Equal(AccessMode.ReadWrite, entity.AccessMode);
        Assert.False(entity.IsEnabled);
        Assert.Equal(1, entity.Id); // Id non cambia
        Assert.Equal(10, entity.DictionaryId); // DictionaryId non cambia
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        // Arrange
        var entities = new List<VariableEntity>
        {
            new() { Id = 1, Name = "Var1", AddressHigh = 0x00, AddressLow = 0x01, 
                    DataTypeKind = DataTypeKind.UInt8, DataTypeRaw = "uint8_t", 
                    AccessMode = AccessMode.ReadOnly, IsEnabled = true },
            new() { Id = 2, Name = "Var2", AddressHigh = 0x00, AddressLow = 0x02, 
                    DataTypeKind = DataTypeKind.UInt16, DataTypeRaw = "uint16_t", 
                    AccessMode = AccessMode.ReadWrite, IsEnabled = true },
            new() { Id = 3, Name = "Var3", AddressHigh = 0x80, AddressLow = 0x01, 
                    DataTypeKind = DataTypeKind.Float, DataTypeRaw = "float", 
                    AccessMode = AccessMode.ReadOnly, IsEnabled = false }
        };

        // Act
        var result = VariableMapper.ToDomainList(entities);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("Var1", result[0].Name);
        Assert.Equal(VariableCategory.Standard, result[0].Category);
        Assert.Equal("Var2", result[1].Name);
        Assert.Equal("Var3", result[2].Name);
        Assert.Equal(VariableCategory.DeviceSpecific, result[2].Category);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        // Arrange
        var originalEntity = new VariableEntity
        {
            Id = 42,
            DictionaryId = 10,
            Name = "RoundTrip",
            AddressHigh = 0x80,
            AddressLow = 0x99,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeParam = 4,
            DataTypeRaw = "Bitmapped[4]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            MinValue = null,
            MaxValue = null,
            Unit = null,
            Usage = "Status flags",
            Description = "System status"
        };

        // Act
        var domain = VariableMapper.ToDomain(originalEntity);
        var resultEntity = VariableMapper.ToEntity(domain, originalEntity.DictionaryId);

        // Assert
        Assert.Equal(originalEntity.Id, resultEntity.Id);
        Assert.Equal(originalEntity.DictionaryId, resultEntity.DictionaryId);
        Assert.Equal(originalEntity.Name, resultEntity.Name);
        Assert.Equal(originalEntity.AddressHigh, resultEntity.AddressHigh);
        Assert.Equal(originalEntity.AddressLow, resultEntity.AddressLow);
        Assert.Equal(originalEntity.DataTypeKind, resultEntity.DataTypeKind);
        Assert.Equal(originalEntity.DataTypeParam, resultEntity.DataTypeParam);
        Assert.Equal(originalEntity.DataTypeRaw, resultEntity.DataTypeRaw);
        Assert.Equal(originalEntity.AccessMode, resultEntity.AccessMode);
        Assert.Equal(originalEntity.IsEnabled, resultEntity.IsEnabled);
    }
}
