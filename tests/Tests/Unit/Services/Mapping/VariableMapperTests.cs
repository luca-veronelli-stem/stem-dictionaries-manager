using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;
using Tests.Shared;

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
            DataTypeRaw = TestData.DataTypes.UInt16,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            Format = "%04X",
            MinValue = 0,
            MaxValue = 65535,
            Unit = null,
            Usage = "Versione firmware",
            Description = "Identificativo versione"
        };

        // Act
        Variable result = VariableMapper.ToDomain(entity);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Firmware macchina", result.Name);
        Assert.Equal(0x00, result.AddressHigh);
        Assert.Equal(0x01, result.AddressLow);
        Assert.Equal(DataTypeKind.UInt16, result.DataTypeKind);
        Assert.Null(result.DataTypeParam);
        Assert.Equal(TestData.DataTypes.UInt16, result.DataTypeRaw);
        Assert.Equal(AccessMode.ReadOnly, result.AccessMode);
        Assert.True(result.IsEnabled);
        Assert.Equal("%04X", result.Format);
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
            DataTypeRaw = TestData.DataTypes.String20,
            AccessMode = AccessMode.ReadWrite,
            IsEnabled = true
        };

        // Act
        Variable result = VariableMapper.ToDomain(entity);

        // Assert
        Assert.Equal(DataTypeKind.String, result.DataTypeKind);
        Assert.Equal(20, result.DataTypeParam);
        Assert.Equal(TestData.DataTypes.String20, result.DataTypeRaw);
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
            DataTypeRaw = TestData.DataTypes.UInt8,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };

        // Act
        Variable result = VariableMapper.ToDomain(entity);

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
            DataTypeRaw = TestData.DataTypes.UInt8,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };

        // Act
        Variable result = VariableMapper.ToDomain(entity);

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
            format: "%.2f",
            minValue: -100.0,
            maxValue: 100.0,
            unit: "°C",
            usage: "Temperature",
            description: "Current temperature");

        // Act
        VariableEntity result = VariableMapper.ToEntity(variable, dictionaryId: 99);

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
        Assert.Equal("%.2f", result.Format);
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
            DataTypeRaw = TestData.DataTypes.UInt8,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };

        var domain = Variable.Restore(
            id: 1,
            name: "NewName",
            addressHigh: 0x80,
            addressLow: 0x02,
            dataTypeKind: DataTypeKind.UInt32,
            dataTypeRaw: TestData.DataTypes.UInt32,
            dataTypeParam: null,
            accessMode: AccessMode.ReadWrite,
            isEnabled: false,
            format: "%d ms",
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
        Assert.Equal(TestData.DataTypes.UInt32, entity.DataTypeRaw);
        Assert.Equal(AccessMode.ReadWrite, entity.AccessMode);
        Assert.False(entity.IsEnabled);
        Assert.Equal("%d ms", entity.Format);
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
                    DataTypeKind = DataTypeKind.UInt8, DataTypeRaw = TestData.DataTypes.UInt8,
                    AccessMode = AccessMode.ReadOnly, IsEnabled = true },
            new() { Id = 2, Name = "Var2", AddressHigh = 0x00, AddressLow = 0x02,
                    DataTypeKind = DataTypeKind.UInt16, DataTypeRaw = TestData.DataTypes.UInt16,
                    AccessMode = AccessMode.ReadWrite, IsEnabled = true },
            new() { Id = 3, Name = "Var3", AddressHigh = 0x80, AddressLow = 0x01,
                    DataTypeKind = DataTypeKind.Float, DataTypeRaw = "float",
                    AccessMode = AccessMode.ReadOnly, IsEnabled = false }
        };

        // Act
        IReadOnlyList<Variable> result = VariableMapper.ToDomainList(entities);

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
            Format = "0x%04X",
            MinValue = null,
            MaxValue = null,
            Unit = null,
            Usage = "Status flags",
            Description = "System status"
        };

        // Act
        Variable domain = VariableMapper.ToDomain(originalEntity);
        VariableEntity resultEntity = VariableMapper.ToEntity(domain, originalEntity.DictionaryId);

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
        Assert.Equal(originalEntity.Format, resultEntity.Format);
    }

    // === WordSize mapping ===

    [Fact]
    public void ToDomain_WithWordSize_MapsCorrectly()
    {
        var entity = new VariableEntity
        {
            Id = 50,
            DictionaryId = 1,
            Name = "Allarmi",
            AddressHigh = 0x00,
            AddressLow = 0x06,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeParam = 2,
            DataTypeRaw = "Bitmapped[2]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            WordSize = 16
        };

        Variable result = VariableMapper.ToDomain(entity);

        Assert.Equal(16, result.WordSize);
    }

    [Fact]
    public void ToDomain_WithoutWordSize_MapsNull()
    {
        var entity = new VariableEntity
        {
            Id = 51,
            DictionaryId = 1,
            Name = "Test",
            AddressHigh = 0x00,
            AddressLow = 0x00,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = TestData.DataTypes.UInt16,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            WordSize = null
        };

        Variable result = VariableMapper.ToDomain(entity);

        Assert.Null(result.WordSize);
    }

    [Fact]
    public void ToEntity_WithWordSize_MapsCorrectly()
    {
        var domain = new Variable("Allarmi", 0x00, 0x06,
            DataTypeKind.Bitmapped, AccessMode.ReadOnly, "Bitmapped[2]",
            dataTypeParam: 2, wordSize: 32);

        VariableEntity entity = VariableMapper.ToEntity(domain, 1);

        Assert.Equal(32, entity.WordSize);
    }

    [Fact]
    public void UpdateEntity_WithWordSize_MapsCorrectly()
    {
        var entity = new VariableEntity
        {
            Id = 52,
            DictionaryId = 1,
            Name = "Old",
            AddressHigh = 0x00,
            AddressLow = 0x06,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = TestData.DataTypes.UInt16,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            WordSize = null
        };
        var domain = new Variable("Allarmi", 0x00, 0x06,
            DataTypeKind.Bitmapped, AccessMode.ReadOnly, "Bitmapped[2]",
            dataTypeParam: 2, wordSize: 8);

        VariableMapper.UpdateEntity(entity, domain);

        Assert.Equal(8, entity.WordSize);
    }
}
