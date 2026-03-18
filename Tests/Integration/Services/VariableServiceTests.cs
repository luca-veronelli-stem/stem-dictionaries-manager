using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per VariableService.
/// </summary>
public class VariableServiceTests : IntegrationTestBase
{
    private readonly VariableService _service;
    private readonly VariableRepository _variableRepo;
    private readonly DictionaryRepository _dictionaryRepo;
    private readonly BitInterpretationRepository _bitInterpretationRepo;
    private DictionaryEntity _testDictionary = null!;

    public VariableServiceTests()
    {
        _dictionaryRepo = new DictionaryRepository(Context);
        _variableRepo = new VariableRepository(Context);
        _bitInterpretationRepo = new BitInterpretationRepository(Context);

        _service = new VariableService(
            _variableRepo,
            _dictionaryRepo,
            _bitInterpretationRepo);
    }

    public override async Task InitializeAsync()
    {
        _testDictionary = new DictionaryEntity { Name = "test-dict" };
        await _dictionaryRepo.AddAsync(_testDictionary);
    }

    // === GetByIdAsync Tests ===

    [Fact]
    public async Task GetByIdAsync_ExistingVariable_ReturnsVariable()
    {
        // Arrange
        var entity = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "TestVar",
            AddressHigh = 0x00,
            AddressLow = 0x10,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "uint16_t",
            AccessMode = AccessMode.ReadWrite,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestVar", result.Name);
        Assert.Equal(DataTypeKind.UInt16, result.DataTypeKind);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // === GetAllAsync Tests ===

    [Fact]
    public async Task GetAllAsync_MultipleVariables_ReturnsAll()
    {
        // Arrange
        await _variableRepo.AddAsync(new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "Var1",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        });
        await _variableRepo.AddAsync(new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "Var2",
            AddressHigh = 0x00,
            AddressLow = 0x02,
            DataTypeKind = DataTypeKind.Int16,
            DataTypeRaw = "int16_t",
            AccessMode = AccessMode.ReadWrite,
            IsEnabled = true
        });

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _service.GetAllAsync();

        Assert.Empty(result);
    }

    // === AddAsync Tests ===

    [Fact]
    public async Task AddAsync_ValidVariable_CreatesAndReturnsVariable()
    {
        // Arrange
        var variable = new Variable(
            name: "NewVar",
            addressHigh: 0x00,
            addressLow: 0x20,
            dataTypeKind: DataTypeKind.Float,
            accessMode: AccessMode.ReadWrite,
            dataTypeRaw: "float",
            usage: "Usage",
            description: "Description");

        // Act
        var result = await _service.AddAsync(_testDictionary.Id, variable);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("NewVar", result.Name);
        Assert.Equal(0x20, result.AddressLow);
    }

    [Fact]
    public async Task AddAsync_DictionaryNotFound_ThrowsKeyNotFoundException()
    {
        var variable = new Variable(
            name: "Orphan",
            addressHigh: 0x00,
            addressLow: 0x30,
            dataTypeKind: DataTypeKind.UInt8,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "uint8_t");

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddAsync(999, variable));
        Assert.Contains("Dictionary", exception.Message);
    }

    [Fact]
    public async Task AddAsync_DuplicateAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        await _variableRepo.AddAsync(new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "Existing",
            AddressHigh = 0x80,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        });

        var variable = new Variable(
            name: "Duplicate",
            addressHigh: 0x80,
            addressLow: 0x01,
            dataTypeKind: DataTypeKind.UInt16,
            accessMode: AccessMode.ReadWrite,
            dataTypeRaw: "uint16_t");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(_testDictionary.Id, variable));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_NullVariable_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AddAsync(_testDictionary.Id, null!));
    }

    // === UpdateAsync Tests ===

    [Fact]
    public async Task UpdateAsync_ExistingVariable_UpdatesFields()
    {
        // Arrange
        var entity = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "Original",
            AddressHigh = 0x00,
            AddressLow = 0x40,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(entity);

        var updated = Variable.Restore(
            id: entity.Id,
            name: "Updated",
            addressHigh: 0x00,
            addressLow: 0x40,
            dataTypeKind: DataTypeKind.UInt16,
            dataTypeRaw: "uint16_t",
            dataTypeParam: null,
            accessMode: AccessMode.ReadWrite,
            isEnabled: false,
            format: null,
            minValue: null,
            maxValue: null,
            unit: null,
            usage: "New Usage",
            description: "New Description");

        // Act
        await _service.UpdateAsync(updated);

        // Assert
        var result = await _service.GetByIdAsync(entity.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
        Assert.Equal(DataTypeKind.UInt16, result.DataTypeKind);
        Assert.Equal(AccessMode.ReadWrite, result.AccessMode);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsKeyNotFoundException()
    {
        var variable = Variable.Restore(
            id: 999,
            name: "Ghost",
            addressHigh: 0x00,
            addressLow: 0x50,
            dataTypeKind: DataTypeKind.UInt8,
            dataTypeRaw: "uint8_t",
            dataTypeParam: null,
            accessMode: AccessMode.ReadOnly,
            isEnabled: true,
            format: null,
            minValue: null,
            maxValue: null,
            unit: null,
            usage: null,
            description: null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(variable));
    }

    [Fact]
    public async Task UpdateAsync_AddressConflict_ThrowsInvalidOperationException()
    {
        // Arrange - crea due variabili
        var first = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "First",
            AddressHigh = 0x00,
            AddressLow = 0x60,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(first);

        var second = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "Second",
            AddressHigh = 0x00,
            AddressLow = 0x61,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(second);

        // Prova a cambiare indirizzo di second a quello di first
        var conflicting = Variable.Restore(
            id: second.Id,
            name: "Second",
            addressHigh: 0x00,
            addressLow: 0x60,
            dataTypeKind: DataTypeKind.UInt8,
            dataTypeRaw: "uint8_t",
            dataTypeParam: null,
            accessMode: AccessMode.ReadOnly,
            isEnabled: true,
            format: null,
            minValue: null,
            maxValue: null,
            unit: null,
            usage: null,
            description: null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(conflicting));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_NullVariable_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.UpdateAsync(null!));
    }

    // === DeleteAsync Tests ===

    [Fact]
    public async Task DeleteAsync_ExistingVariable_RemovesIt()
    {
        // Arrange
        var entity = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "ToDelete",
            AddressHigh = 0x00,
            AddressLow = 0x70,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(entity);

        // Act
        await _service.DeleteAsync(entity.Id);

        // Assert
        var result = await _service.GetByIdAsync(entity.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(999));
    }

    // === GetByDictionaryIdAsync Tests ===

    [Fact]
    public async Task GetByDictionaryIdAsync_ReturnsOnlyDictionaryVariables()
    {
        // Arrange - crea secondo dizionario
        var otherDict = new DictionaryEntity { Name = "other-dict" };
        await _dictionaryRepo.AddAsync(otherDict);

        await _variableRepo.AddAsync(new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "InTest",
            AddressHigh = 0x00,
            AddressLow = 0x80,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        });
        await _variableRepo.AddAsync(new VariableEntity
        {
            DictionaryId = otherDict.Id,
            Name = "InOther",
            AddressHigh = 0x00,
            AddressLow = 0x81,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        });

        // Act
        var result = await _service.GetByDictionaryIdAsync(_testDictionary.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal("InTest", result[0].Name);
    }

    // === GetByAddressAsync Tests ===

    [Fact]
    public async Task GetByAddressAsync_Found_ReturnsVariable()
    {
        // Arrange
        await _variableRepo.AddAsync(new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "AtAddress",
            AddressHigh = 0x80,
            AddressLow = 0x90,
            DataTypeKind = DataTypeKind.Float,
            DataTypeRaw = "float",
            AccessMode = AccessMode.ReadWrite,
            IsEnabled = true
        });

        // Act
        var result = await _service.GetByAddressAsync(_testDictionary.Id, 0x80, 0x90);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AtAddress", result.Name);
    }

    [Fact]
    public async Task GetByAddressAsync_NotFound_ReturnsNull()
    {
        var result = await _service.GetByAddressAsync(_testDictionary.Id, 0xFF, 0xFF);

        Assert.Null(result);
    }

    // === BitInterpretation Tests ===

    [Fact]
    public async Task GetBitInterpretationsAsync_ReturnsInterpretations()
    {
        // Arrange - variabile bitmapped
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "Bitmapped",
            AddressHigh = 0x00,
            AddressLow = 0xA0,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[1]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = variable.Id,
            DeviceType = DeviceType.OptimusXp,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Motor"
        });
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = variable.Id,
            DeviceType = DeviceType.OptimusXp,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Error"
        });

        // Act
        var result = await _service.GetBitInterpretationsAsync(variable.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetBitInterpretationsAsync_VariableNotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetBitInterpretationsAsync(999));
    }

    [Fact]
    public async Task AddBitInterpretationAsync_ValidBitmapped_AddsInterpretation()
    {
        // Arrange - variabile bitmapped
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "Bits",
            AddressHigh = 0x00,
            AddressLow = 0xB0,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[2]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        var interpretation = new BitInterpretation(
            variableId: variable.Id,
            deviceType: DeviceType.Eden,
            wordIndex: 0,
            bitIndex: 5,
            meaning: "Pump Active");

        // Act
        var result = await _service.AddBitInterpretationAsync(variable.Id, interpretation);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("Pump Active", result.Meaning);
        Assert.Equal(5, result.BitIndex);
    }

    [Fact]
    public async Task AddBitInterpretationAsync_VariableNotFound_ThrowsKeyNotFoundException()
    {
        var interpretation = new BitInterpretation(
            variableId: 999,
            deviceType: DeviceType.Spark,
            wordIndex: 0,
            bitIndex: 0,
            meaning: "Test");

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddBitInterpretationAsync(999, interpretation));
    }

    [Fact]
    public async Task AddBitInterpretationAsync_NotBitmapped_ThrowsInvalidOperationException()
    {
        // Arrange - variabile NON bitmapped
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "NotBits",
            AddressHigh = 0x00,
            AddressLow = 0xC0,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "uint16_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        var interpretation = new BitInterpretation(
            variableId: variable.Id,
            deviceType: DeviceType.Gradino,
            wordIndex: 0,
            bitIndex: 0,
            meaning: "Test");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddBitInterpretationAsync(variable.Id, interpretation));
        Assert.Contains("not bitmapped", exception.Message);
    }

    [Fact]
    public async Task AddBitInterpretationAsync_NullInterpretation_ThrowsArgumentNullException()
    {
        // Arrange - variabile bitmapped
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "NullTest",
            AddressHigh = 0x00,
            AddressLow = 0xD0,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[1]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.AddBitInterpretationAsync(variable.Id, null!));
    }
}
