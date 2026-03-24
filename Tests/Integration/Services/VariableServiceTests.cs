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
    private readonly VariableDeviceStateRepository _deviceStateRepo;
    private DictionaryEntity _testDictionary = null!;

    public VariableServiceTests()
    {
        _dictionaryRepo = new DictionaryRepository(Context);
        _variableRepo = new VariableRepository(Context);
        _bitInterpretationRepo = new BitInterpretationRepository(Context);
        _deviceStateRepo = new VariableDeviceStateRepository(Context);

        _service = new VariableService(
            _variableRepo,
            _dictionaryRepo,
            _bitInterpretationRepo,
            _deviceStateRepo);
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
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Motor"
        });
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = variable.Id,
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

        var interpretation = new BitInterpretation(variableId: variable.Id, wordIndex: 0,
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
        var interpretation = new BitInterpretation(variableId: 999, wordIndex: 0,
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

        var interpretation = new BitInterpretation(variableId: variable.Id, wordIndex: 0,
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

    [Fact]
    public async Task UpdateBitInterpretationsAsync_ReplacesExisting_AndAddsNew()
    {
        // Arrange
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "UpdateBits",
            AddressHigh = 0x00,
            AddressLow = 0xE0,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[1]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        // Aggiungi interpretazioni iniziali
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = variable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Old Motor"
        });
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = variable.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "To Delete"
        });

        // Nuove interpretazioni (0 modificata, 1 eliminata, 2 nuova)
        var newInterpretations = new List<BitInterpretation>
        {
            new(variable.Id, 0, 0, "Updated Motor"),
            new(variable.Id, 0, 2, "New Error")
        };

        // Act
        await _service.UpdateBitInterpretationsAsync(variable.Id, newInterpretations);
        var result = await _service.GetBitInterpretationsAsync(variable.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.BitIndex == 0 && r.Meaning == "Updated Motor");
        Assert.Contains(result, r => r.BitIndex == 2 && r.Meaning == "New Error");
        Assert.DoesNotContain(result, r => r.BitIndex == 1);
    }

    [Fact]
    public async Task UpdateBitInterpretationsAsync_VariableNotFound_ThrowsKeyNotFoundException()
    {
        var interpretations = new List<BitInterpretation>
        {
            new(999, 0, 0, "Test")
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateBitInterpretationsAsync(999, interpretations));
    }

    [Fact]
    public async Task UpdateBitInterpretationsAsync_NotBitmapped_ThrowsInvalidOperationException()
    {
        // Arrange - variabile NON bitmapped
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "NotBitmapped",
            AddressHigh = 0x00,
            AddressLow = 0xE1,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "uint16_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        var interpretations = new List<BitInterpretation>
        {
            new(variable.Id, 0, 0, "Test")
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateBitInterpretationsAsync(variable.Id, interpretations));
        Assert.Contains("not bitmapped", ex.Message);
    }

    [Fact]
    public async Task UpdateBitInterpretationsAsync_DuplicateKeys_ThrowsInvalidOperationException()
    {
        // Arrange
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "DupKeys",
            AddressHigh = 0x00,
            AddressLow = 0xE2,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[1]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        var interpretations = new List<BitInterpretation>
        {
            new(variable.Id, 0, 0, "First"),
            new(variable.Id, 0, 0, "Duplicate")
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateBitInterpretationsAsync(variable.Id, interpretations));
        Assert.Contains("Duplicate", ex.Message);
    }

    [Fact]
    public async Task UpdateBitInterpretationsAsync_NullInterpretations_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.UpdateBitInterpretationsAsync(1, null!));
    }

    [Fact]
    public async Task UpdateBitInterpretationsAsync_PreservesExistingIds()
    {
        // Arrange
        var variable = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "PreserveIds",
            AddressHigh = 0x00,
            AddressLow = 0xE3,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[1]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = variable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Motor"
        });
        var originalBits = await _service.GetBitInterpretationsAsync(variable.Id);
        var originalId = originalBits[0].Id;

        // Act - aggiorna meaning, stessa chiave
        var updated = new List<BitInterpretation>
        {
            new(variable.Id, 0, 0, "Motor Updated")
        };
        await _service.UpdateBitInterpretationsAsync(variable.Id, updated);

        var result = await _service.GetBitInterpretationsAsync(variable.Id);

        // Assert - ID preservato
        Assert.Single(result);
        Assert.Equal(originalId, result[0].Id);
        Assert.Equal("Motor Updated", result[0].Meaning);
    }

    // === DeviceState Tests ===

    [Fact]
    public async Task SetDeviceStateAsync_CreatesNewState()
    {
        var variable = await CreateTestVariable();

        await _service.SetDeviceStateAsync(variable.Id, DeviceType.SherpaSlim, false);

        var state = await _service.GetDeviceStateAsync(variable.Id, DeviceType.SherpaSlim);
        Assert.NotNull(state);
        Assert.False(state.IsEnabled);
    }

    [Fact]
    public async Task SetDeviceStateAsync_UpdatesExistingState()
    {
        var variable = await CreateTestVariable();
        await _service.SetDeviceStateAsync(variable.Id, DeviceType.SherpaSlim, false);

        await _service.SetDeviceStateAsync(variable.Id, DeviceType.SherpaSlim, true);

        var state = await _service.GetDeviceStateAsync(variable.Id, DeviceType.SherpaSlim);
        Assert.NotNull(state);
        Assert.True(state.IsEnabled);
    }

    [Fact]
    public async Task SetDeviceStateAsync_VariableNotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SetDeviceStateAsync(999, DeviceType.Spark, false));
    }

    [Fact]
    public async Task SetDeviceStateAsync_BR011_EnableOnDeprecated_ThrowsInvalidOperationException()
    {
        // Crea variabile disabilitata (deprecata)
        var variable = await CreateTestVariable(isEnabled: false);

        // BR-011: tentativo di abilitare per un device → errore
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetDeviceStateAsync(variable.Id, DeviceType.OptimusXp, true));

        Assert.Contains("deprecated globally", ex.Message);
    }

    [Fact]
    public async Task SetDeviceStateAsync_BR011_DisableOnDeprecated_Allowed()
    {
        var variable = await CreateTestVariable(isEnabled: false);

        // Disabilitare per device su variabile deprecata è consentito (no-op logico ma valido)
        await _service.SetDeviceStateAsync(variable.Id, DeviceType.OptimusXp, false);

        var state = await _service.GetDeviceStateAsync(variable.Id, DeviceType.OptimusXp);
        Assert.NotNull(state);
        Assert.False(state.IsEnabled);
    }

    [Fact]
    public async Task GetDeviceStateAsync_NotExists_ReturnsNull()
    {
        var variable = await CreateTestVariable();

        var state = await _service.GetDeviceStateAsync(variable.Id, DeviceType.Spark);

        Assert.Null(state);
    }

    [Fact]
    public async Task GetDeviceStatesAsync_ReturnsAllOverrides()
    {
        var variable = await CreateTestVariable();
        await _service.SetDeviceStateAsync(variable.Id, DeviceType.SherpaSlim, false);
        await _service.SetDeviceStateAsync(variable.Id, DeviceType.Gradino, false);

        var states = await _service.GetDeviceStatesAsync(variable.Id);

        Assert.Equal(2, states.Count);
    }

    [Fact]
    public async Task GetDeviceStatesAsync_NoOverrides_ReturnsEmpty()
    {
        var variable = await CreateTestVariable();

        var states = await _service.GetDeviceStatesAsync(variable.Id);

        Assert.Empty(states);
    }

    private async Task<Variable> CreateTestVariable(
        byte addressLow = 0x01,
        bool isEnabled = true)
    {
        var variable = new Variable(
            "TestVar", 0x00, addressLow,
            DataTypeKind.UInt16, AccessMode.ReadWrite, "UInt16",
            isEnabled: isEnabled);
        return await _service.AddAsync(_testDictionary.Id, variable);
    }
}
