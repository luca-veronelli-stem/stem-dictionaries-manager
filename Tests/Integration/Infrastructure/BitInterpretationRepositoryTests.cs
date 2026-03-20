using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per BitInterpretationRepository.
/// </summary>
public class BitInterpretationRepositoryTests : IntegrationTestBase
{
    private readonly BitInterpretationRepository _repository;
    private readonly VariableRepository _variableRepo;
    private readonly DictionaryRepository _dictionaryRepo;
    private VariableEntity _testVariable = null!;

    public BitInterpretationRepositoryTests()
    {
        _repository = new BitInterpretationRepository(Context);
        _variableRepo = new VariableRepository(Context);
        _dictionaryRepo = new DictionaryRepository(Context);
    }

    public override async Task InitializeAsync()
    {
        // Setup: crea dizionario e variabile bitmapped
        var dictionary = new DictionaryEntity { Name = "test-dict" };
        await _dictionaryRepo.AddAsync(dictionary);

        _testVariable = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "StatusBits",
            AddressHigh = 0x00,
            AddressLow = 0x10,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[2]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(_testVariable);
    }

    [Fact]
    public async Task AddAsync_CreatesInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Motor Running"
        };

        var result = await _repository.AddAsync(interpretation);

        Assert.True(result.Id > 0);
        Assert.Equal("Motor Running", result.Meaning);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Error Flag"
        };
        await _repository.AddAsync(interpretation);

        var result = await _repository.GetByIdAsync(interpretation.Id);

        Assert.NotNull(result);
        Assert.Equal("Error Flag", result.Meaning);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByVariableIdAsync_ReturnsInterpretations()
    {
        // Arrange - aggiungi multiple interpretazioni
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Bit 0"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Bit 1"
        });

        // Act
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByVariableIdAsync_NoResults_ReturnsEmptyList()
    {
        var result = await _repository.GetByVariableIdAsync(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByVariableIdAsync_OrdersByBitIndex()
    {
        // Arrange - aggiungi in ordine inverso
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 5,
            Meaning = "Bit 5"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 2,
            Meaning = "Bit 2"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 8,
            Meaning = "Bit 8"
        });

        // Act
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert - deve essere ordinato per BitIndex
        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].BitIndex);
        Assert.Equal(5, result[1].BitIndex);
        Assert.Equal(8, result[2].BitIndex);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 3,
            Meaning = "Original"
        };
        await _repository.AddAsync(interpretation);

        interpretation.Meaning = "Updated Meaning";
        await _repository.UpdateAsync(interpretation);

        var result = await _repository.GetByIdAsync(interpretation.Id);
        Assert.Equal("Updated Meaning", result!.Meaning);
    }

    [Fact]
    public async Task DeleteAsync_RemovesInterpretation()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 1,
            BitIndex = 0,
            Meaning = "To Delete"
        };
        await _repository.AddAsync(interpretation);

        await _repository.DeleteAsync(interpretation.Id);

        var result = await _repository.GetByIdAsync(interpretation.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }

    // === SyncByVariableIdAsync Tests ===

    [Fact]
    public async Task SyncByVariableIdAsync_AddsNewAndDeletesMissing()
    {
        // Arrange - aggiungi interpretazione iniziale
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "To Delete"
        });

        // Incoming: solo bit 1 (nuovo), bit 0 non presente → deve essere eliminato
        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 1, Meaning = "New Bit" }
        };

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, incoming);
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].BitIndex);
        Assert.Equal("New Bit", result[0].Meaning);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_UpdatesExistingMeaning()
    {
        // Arrange
        var existing = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Old Meaning"
        };
        await _repository.AddAsync(existing);
        var existingId = existing.Id;

        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "Updated Meaning" }
        };

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, incoming);
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert - stessa riga aggiornata, ID preservato
        Assert.Single(result);
        Assert.Equal(existingId, result[0].Id);
        Assert.Equal("Updated Meaning", result[0].Meaning);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_PreservesIdForUnchangedRows()
    {
        // Arrange
        var existing = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 3,
            Meaning = "Unchanged"
        };
        await _repository.AddAsync(existing);
        var existingId = existing.Id;

        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 3, Meaning = "Unchanged" }
        };

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, incoming);
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert - ID preservato, meaning invariato
        Assert.Single(result);
        Assert.Equal(existingId, result[0].Id);
        Assert.Equal("Unchanged", result[0].Meaning);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_EmptyIncoming_DeletesAll()
    {
        // Arrange
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Will Delete"
        });

        // Act
        await _repository.SyncByVariableIdAsync(_testVariable.Id, []);
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert
        Assert.Empty(result);
    }
}
