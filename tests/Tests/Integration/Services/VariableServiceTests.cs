using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services;
using Services.Interfaces;

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
    private readonly StandardVariableOverrideRepository _overrideRepo;
    private DictionaryEntity _testDictionary = null!;

    public VariableServiceTests()
    {
        SeedTestUser();
        _dictionaryRepo = new DictionaryRepository(Context);
        _variableRepo = new VariableRepository(Context);
        _bitInterpretationRepo = new BitInterpretationRepository(Context);
        _overrideRepo = new StandardVariableOverrideRepository(Context);
        var auditRepository = new AuditEntryRepository(Context);
        IAuditService auditService = new AuditService(auditRepository);
        ICurrentUserProvider userProvider = new CurrentUserProvider { CurrentUserId = 1 };

        _service = new VariableService(
            _variableRepo,
            _dictionaryRepo,
            _bitInterpretationRepo,
            _overrideRepo,
            auditService,
            userProvider);
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
        Variable? result = await _service.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestVar", result.Name);
        Assert.Equal(DataTypeKind.UInt16, result.DataTypeKind);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        Variable? result = await _service.GetByIdAsync(999);

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
        IReadOnlyList<Variable> result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        IReadOnlyList<Variable> result = await _service.GetAllAsync();

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
        Variable result = await _service.AddAsync(_testDictionary.Id, variable);

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

        KeyNotFoundException exception = await Assert.ThrowsAsync<KeyNotFoundException>(
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
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
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
        Variable? result = await _service.GetByIdAsync(entity.Id);
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
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
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
        Variable? result = await _service.GetByIdAsync(entity.Id);
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
        IReadOnlyList<Variable> result = await _service.GetByDictionaryIdAsync(_testDictionary.Id);

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
        Variable? result = await _service.GetByAddressAsync(_testDictionary.Id, 0x80, 0x90);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("AtAddress", result.Name);
    }

    [Fact]
    public async Task GetByAddressAsync_NotFound_ReturnsNull()
    {
        Variable? result = await _service.GetByAddressAsync(_testDictionary.Id, 0xFF, 0xFF);

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
        IReadOnlyList<BitInterpretation> result = await _service.GetBitInterpretationsAsync(variable.Id);

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
            meaning: "Pump Active", dictionaryId: null);

        // Act
        BitInterpretation result = await _service.AddBitInterpretationAsync(variable.Id, interpretation);

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
            meaning: "Test", dictionaryId: null);

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
            meaning: "Test", dictionaryId: null);

        // Act & Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
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
            new(variable.Id, 0, 0, "Updated Motor", null),
            new(variable.Id, 0, 2, "New Error", null)
        };

        // Act
        await _service.UpdateBitInterpretationsAsync(variable.Id, newInterpretations);
        IReadOnlyList<BitInterpretation> result = await _service.GetBitInterpretationsAsync(variable.Id);

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
            new(999, 0, 0, "Test", null)
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
            new(variable.Id, 0, 0, "Test", null)
        };

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
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
            new(variable.Id, 0, 0, "First", null),
            new(variable.Id, 0, 0, "Duplicate", null)
        };

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
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
        IReadOnlyList<BitInterpretation> originalBits = await _service.GetBitInterpretationsAsync(variable.Id);
        int originalId = originalBits[0].Id;

        // Act - aggiorna meaning, stessa chiave
        var updated = new List<BitInterpretation>
        {
            new(variable.Id, 0, 0, "Motor Updated", null)
        };
        await _service.UpdateBitInterpretationsAsync(variable.Id, updated);

        IReadOnlyList<BitInterpretation> result = await _service.GetBitInterpretationsAsync(variable.Id);

        // Assert - ID preservato
        Assert.Single(result);
        Assert.Equal(originalId, result[0].Id);
        Assert.Equal("Motor Updated", result[0].Meaning);
    }

    // === StandardVariableOverride Tests (v7) ===

    [Fact]
    public async Task SetOverrideAsync_CreatesNewOverride()
    {
        Variable variable = await CreateTestVariable();

        await _service.SetOverrideAsync(dictionaryId: 1, variable.Id, isEnabled: false);

        StandardVariableOverride? overrideState = await _service.GetOverrideAsync(1, variable.Id);
        Assert.NotNull(overrideState);
        Assert.False(overrideState.IsEnabled);
    }

    [Fact]
    public async Task SetOverrideAsync_UpdatesExistingOverride()
    {
        Variable variable = await CreateTestVariable();
        await _service.SetOverrideAsync(1, variable.Id, false);

        await _service.SetOverrideAsync(1, variable.Id, true);

        StandardVariableOverride? overrideState = await _service.GetOverrideAsync(1, variable.Id);
        Assert.NotNull(overrideState);
        Assert.True(overrideState.IsEnabled);
    }

    [Fact]
    public async Task SetOverrideAsync_VariableNotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SetOverrideAsync(1, 999, false));
    }

    [Fact]
    public async Task SetOverrideAsync_BR011_EnableOnDeprecated_ThrowsInvalidOperationException()
    {
        // Crea variabile disabilitata (deprecata)
        Variable variable = await CreateTestVariable(isEnabled: false);

        // BR-011: tentativo di abilitare override → errore
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetOverrideAsync(1, variable.Id, true));

        Assert.Contains("deprecated globally", ex.Message);
    }

    [Fact]
    public async Task SetOverrideAsync_BR011_DisableOnDeprecated_Allowed()
    {
        Variable variable = await CreateTestVariable(isEnabled: false);

        // Disabilitare override su variabile deprecata è consentito
        await _service.SetOverrideAsync(1, variable.Id, false);

        StandardVariableOverride? overrideState = await _service.GetOverrideAsync(1, variable.Id);
        Assert.NotNull(overrideState);
        Assert.False(overrideState.IsEnabled);
    }

    [Fact]
    public async Task GetOverrideAsync_NotExists_ReturnsNull()
    {
        Variable variable = await CreateTestVariable();

        StandardVariableOverride? overrideState = await _service.GetOverrideAsync(1, variable.Id);

        Assert.Null(overrideState);
    }

    [Fact]
    public async Task GetOverridesByVariableAsync_ReturnsAllOverrides()
    {
        Variable variable = await CreateTestVariable();
        var dict2 = new DictionaryEntity { Name = "override-dict-2" };
        await _dictionaryRepo.AddAsync(dict2);

        await _service.SetOverrideAsync(_testDictionary.Id, variable.Id, false);
        await _service.SetOverrideAsync(dict2.Id, variable.Id, false);

        IReadOnlyList<StandardVariableOverride> overrides = await _service.GetOverridesByVariableAsync(variable.Id);

        Assert.Equal(2, overrides.Count);
    }

    [Fact]
    public async Task GetOverridesByVariableAsync_NoOverrides_ReturnsEmpty()
    {
        Variable variable = await CreateTestVariable();

        IReadOnlyList<StandardVariableOverride> overrides = await _service.GetOverridesByVariableAsync(variable.Id);

        Assert.Empty(overrides);
    }

    // === GetOverridesByDictionaryAsync ===

    [Fact]
    public async Task GetOverridesByDictionaryAsync_ReturnsOverridesForDictionary()
    {
        Variable var1 = await CreateTestVariable(addressLow: 0x60);
        Variable var2 = await CreateTestVariable(addressLow: 0x61);
        await _service.SetOverrideAsync(_testDictionary.Id, var1.Id, false);
        await _service.SetOverrideAsync(_testDictionary.Id, var2.Id, true);

        IReadOnlyList<StandardVariableOverride> result = await _service.GetOverridesByDictionaryAsync(_testDictionary.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(_testDictionary.Id, o.DictionaryId));
    }

    [Fact]
    public async Task GetOverridesByDictionaryAsync_ExcludesOtherDictionaries()
    {
        Variable variable = await CreateTestVariable(addressLow: 0x62);
        var dict2 = new DictionaryEntity { Name = "override-dict-other" };
        await _dictionaryRepo.AddAsync(dict2);

        await _service.SetOverrideAsync(_testDictionary.Id, variable.Id, false);
        await _service.SetOverrideAsync(dict2.Id, variable.Id, true);

        IReadOnlyList<StandardVariableOverride> result = await _service.GetOverridesByDictionaryAsync(_testDictionary.Id);

        Assert.Single(result);
        Assert.Equal(variable.Id, result[0].StandardVariableId);
        Assert.False(result[0].IsEnabled);
    }

    [Fact]
    public async Task GetOverridesByDictionaryAsync_NoOverrides_ReturnsEmptyList()
    {
        await CreateTestVariable(addressLow: 0x63);

        IReadOnlyList<StandardVariableOverride> result = await _service.GetOverridesByDictionaryAsync(99);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOverridesByDictionaryAsync_MapsToDomainCorrectly()
    {
        Variable variable = await CreateTestVariable(addressLow: 0x64);
        var dictForOverride = new DictionaryEntity { Name = "override-dict-map" };
        await _dictionaryRepo.AddAsync(dictForOverride);

        await _service.SetOverrideAsync(dictForOverride.Id, variable.Id, false);

        IReadOnlyList<StandardVariableOverride> result = await _service.GetOverridesByDictionaryAsync(dictForOverride.Id);

        StandardVariableOverride overrideState = Assert.Single(result);
        Assert.Equal(variable.Id, overrideState.StandardVariableId);
        Assert.Equal(dictForOverride.Id, overrideState.DictionaryId);
        Assert.False(overrideState.IsEnabled);
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

    // === GetBitInterpretationsAsync (normal mode — solo comuni) ===

    [Fact]
    public async Task GetBitInterpretationsAsync_ExcludesDictionarySpecific()
    {
        // Arrange — bit comuni + dictionary-specific
        var nonStdDict = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(nonStdDict);
        await Context.SaveChangesAsync();

        var variable = new Variable(
            "MixedBits", 0x00, 0x73,
            DataTypeKind.Bitmapped, AccessMode.ReadOnly, "bitmapped[1]",
            dataTypeParam: 1, isEnabled: true);
        Variable created = await _service.AddAsync(_testDictionary.Id, variable);

        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit 0",
            DictionaryId = null
        });
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Dictionary bit 0",
            DictionaryId = nonStdDict.Id
        });
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Common bit 1",
            DictionaryId = null
        });

        // Act — normal mode (no dictionary)
        IReadOnlyList<BitInterpretation> result = await _service.GetBitInterpretationsAsync(created.Id);

        // Assert — solo le comuni, nessuna dictionary-specific
        Assert.Equal(2, result.Count);
        Assert.All(result, b => Assert.Null(b.DictionaryId));
        Assert.Contains(result, b => b.BitIndex == 0 && b.Meaning == "Common bit 0");
        Assert.Contains(result, b => b.BitIndex == 1 && b.Meaning == "Common bit 1");
    }

    // === GetBitInterpretationsForDictionaryAsync merge/override (v7) ===

    [Fact]
    public async Task GetBitInterpretationsForDictionaryAsync_MergesDictionaryOverCommon()
    {
        // Arrange — variabile bitmapped con bit comuni + dictionary-specific
        var nonStdDict = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(nonStdDict);
        await Context.SaveChangesAsync();

        var variable = new Variable(
            "StatusBits", 0x00, 0x70,
            DataTypeKind.Bitmapped, AccessMode.ReadOnly, "bitmapped[1]",
            dataTypeParam: 1, isEnabled: true);
        Variable created = await _service.AddAsync(_testDictionary.Id, variable);

        // Bit comuni (DictionaryId = null)
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit 0",
            DictionaryId = null
        });
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Common bit 1",
            DictionaryId = null
        });
        // Override per dizionario: solo bit 0
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Dictionary bit 0",
            DictionaryId = nonStdDict.Id
        });

        // Act
        IReadOnlyList<BitInterpretation> result = await _service.GetBitInterpretationsForDictionaryAsync(created.Id, nonStdDict.Id);

        // Assert — 2 bit: bit 0 = dictionary override, bit 1 = common fallback
        Assert.Equal(2, result.Count);
        Assert.Equal("Dictionary bit 0", result.First(b => b.BitIndex == 0).Meaning);
        Assert.Equal("Common bit 1", result.First(b => b.BitIndex == 1).Meaning);
    }

    [Fact]
    public async Task GetBitInterpretationsForDictionaryAsync_NoOverride_ReturnsCommon()
    {
        // Arrange — solo bit comuni, nessun override per dizionario 99
        var variable = new Variable(
            "StatusBits2", 0x00, 0x71,
            DataTypeKind.Bitmapped, AccessMode.ReadOnly, "bitmapped[1]",
            dataTypeParam: 1, isEnabled: true);
        Variable created = await _service.AddAsync(_testDictionary.Id, variable);

        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common only",
            DictionaryId = null
        });

        // Act
        IReadOnlyList<BitInterpretation> result = await _service.GetBitInterpretationsForDictionaryAsync(created.Id, 99);

        // Assert — fallback alle comuni
        Assert.Single(result);
        Assert.Equal("Common only", result[0].Meaning);
        Assert.Null(result[0].DictionaryId);
    }

    [Fact]
    public async Task GetBitInterpretationsForDictionaryAsync_AllOverridden_ReturnsOnlyDictionary()
    {
        // Arrange — tutti i bit hanno override per dizionario
        var nonStdDict = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(nonStdDict);
        await Context.SaveChangesAsync();

        var variable = new Variable(
            "StatusBits3", 0x00, 0x72,
            DataTypeKind.Bitmapped, AccessMode.ReadOnly, "bitmapped[1]",
            dataTypeParam: 1, isEnabled: true);
        Variable created = await _service.AddAsync(_testDictionary.Id, variable);

        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit 0",
            DictionaryId = null
        });
        await _bitInterpretationRepo.AddAsync(new BitInterpretationEntity
        {
            VariableId = created.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Dictionary bit 0",
            DictionaryId = nonStdDict.Id
        });

        // Act
        IReadOnlyList<BitInterpretation> result = await _service.GetBitInterpretationsForDictionaryAsync(created.Id, nonStdDict.Id);

        // Assert — solo 1 bit (dictionary override, non duplicato)
        Assert.Single(result);
        Assert.Equal("Dictionary bit 0", result[0].Meaning);
        Assert.Equal(nonStdDict.Id, result[0].DictionaryId);
    }

    // === WordSize Persistence Tests ===

    [Fact]
    public async Task AddAsync_Bitmapped_WithWordSize_PersistsWordSize()
    {
        // Arrange
        var variable = new Variable(
            name: "StatusFlags",
            addressHigh: 0x00,
            addressLow: 0xA0,
            dataTypeKind: DataTypeKind.Bitmapped,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "Bitmapped[2]",
            dataTypeParam: 2,
            description: "Bitmapped with wordSize",
            wordSize: 8);

        // Act
        Variable created = await _service.AddAsync(_testDictionary.Id, variable);

        // Assert — round-trip: reload from DB
        Variable? loaded = await _service.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal(8, loaded.WordSize);
        Assert.Equal(DataTypeKind.Bitmapped, loaded.DataTypeKind);
        Assert.Equal(2, loaded.DataTypeParam);
    }

    [Fact]
    public async Task UpdateAsync_Bitmapped_WordSize_UpdatesInDb()
    {
        // Arrange — crea con wordSize=16
        var entity = new VariableEntity
        {
            DictionaryId = _testDictionary.Id,
            Name = "AlarmFlags",
            AddressHigh = 0x00,
            AddressLow = 0xA1,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "Bitmapped[1]",
            DataTypeParam = 1,
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true,
            WordSize = 16
        };
        await _variableRepo.AddAsync(entity);

        // Act — aggiorna wordSize a 32
        var updated = Variable.Restore(
            id: entity.Id,
            name: "AlarmFlags",
            addressHigh: 0x00,
            addressLow: 0xA1,
            dataTypeKind: DataTypeKind.Bitmapped,
            dataTypeRaw: "Bitmapped[1]",
            dataTypeParam: 1,
            accessMode: AccessMode.ReadOnly,
            isEnabled: true,
            format: null,
            minValue: null,
            maxValue: null,
            unit: null,
            usage: null,
            description: null,
            wordSize: 32);

        await _service.UpdateAsync(updated);

        // Assert
        Variable? loaded = await _service.GetByIdAsync(entity.Id);
        Assert.NotNull(loaded);
        Assert.Equal(32, loaded.WordSize);
    }
}
