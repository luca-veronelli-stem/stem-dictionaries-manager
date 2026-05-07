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
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);
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
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);
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
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);
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
        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, []);
        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        // Assert
        Assert.Empty(result);
    }

    // === DictionaryId Tests (v7) ===

    [Fact]
    public async Task AddAsync_WithDictionaryId_PersistsDictionaryId()
    {
        var dictionary = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(dictionary);
        await Context.SaveChangesAsync();

        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DictionaryId = dictionary.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Dictionary-specific bit"
        };

        var result = await _repository.AddAsync(interpretation);

        Assert.Equal(dictionary.Id, result.DictionaryId);
    }

    [Fact]
    public async Task AddAsync_WithNullDictionaryId_PersistsNull()
    {
        var interpretation = new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DictionaryId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit"
        };

        var result = await _repository.AddAsync(interpretation);

        Assert.Null(result.DictionaryId);
    }

    [Fact]
    public async Task GetByVariableAndDictionaryAsync_ReturnsBothCommonAndDictionarySpecific()
    {
        var dictionary = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(dictionary);
        await Context.SaveChangesAsync();

        // Common interpretation (DictionaryId = null)
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DictionaryId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common bit 0"
        });

        // Dictionary-specific interpretation
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DictionaryId = dictionary.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Dictionary bit 1"
        });

        // Another dictionary's interpretation (should NOT appear)
        var otherDict = new DictionaryEntity { Name = "OtherDict", IsStandard = false };
        Context.Dictionaries.Add(otherDict);
        await Context.SaveChangesAsync();

        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DictionaryId = otherDict.Id,
            WordIndex = 0,
            BitIndex = 2,
            Meaning = "Other dictionary bit"
        });

        var result = await _repository.GetByVariableAndDictionaryAsync(_testVariable.Id, dictionary.Id);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Meaning == "Common bit 0" && r.DictionaryId == null);
        Assert.Contains(result, r => r.Meaning == "Dictionary bit 1" && r.DictionaryId == dictionary.Id);
    }

    [Fact]
    public async Task GetByVariableAndDictionaryAsync_NoDictionaryOverrides_ReturnsOnlyCommon()
    {
        var dictionary = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(dictionary);
        await Context.SaveChangesAsync();

        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id,
            DictionaryId = null,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Common only"
        });

        var result = await _repository.GetByVariableAndDictionaryAsync(_testVariable.Id, dictionary.Id);

        Assert.Single(result);
        Assert.Null(result[0].DictionaryId);
    }

    [Fact]
    public async Task GetByVariableIdAsync_ReturnsAllDictionaries()
    {
        var dictionary = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(dictionary);
        await Context.SaveChangesAsync();

        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = dictionary.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Dictionary override"
        });

        var result = await _repository.GetByVariableIdAsync(_testVariable.Id);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SyncByVariableIdAsync_WithDictionaryId_OnlySyncsForThatDictionary()
    {
        var dictionary = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(dictionary);
        await Context.SaveChangesAsync();

        // Pre-existing: common + dictionary-specific
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = dictionary.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Old dictionary"
        });

        // Sync only dictionary-specific: replace with new interpretation
        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "New dictionary" },
            new() { WordIndex = 0, BitIndex = 1, Meaning = "New dictionary bit 1" }
        };

        await _repository.SyncByVariableIdAsync(_testVariable.Id, dictionary.Id, incoming);

        // Common should be untouched
        var all = await _repository.GetByVariableIdAsync(_testVariable.Id);
        var common = all.Where(r => r.DictionaryId == null).ToList();
        var dictSpecific = all.Where(r => r.DictionaryId == dictionary.Id).ToList();

        Assert.Single(common);
        Assert.Equal("Common", common[0].Meaning);
        Assert.Equal(2, dictSpecific.Count);
        Assert.Contains(dictSpecific, r => r.Meaning == "New dictionary");
        Assert.Contains(dictSpecific, r => r.Meaning == "New dictionary bit 1");
    }

    [Fact]
    public async Task SyncByVariableIdAsync_NullDictionaryId_OnlySyncsCommon()
    {
        var dictionary = new DictionaryEntity { Name = "TestDict", IsStandard = false };
        Context.Dictionaries.Add(dictionary);
        await Context.SaveChangesAsync();

        // Pre-existing: common + dictionary-specific
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Old common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = dictionary.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Dictionary specific"
        });

        // Sync common: replace
        var incoming = new List<BitInterpretationEntity>
        {
            new() { WordIndex = 0, BitIndex = 0, Meaning = "New common" }
        };

        await _repository.SyncByVariableIdAsync(_testVariable.Id, null, incoming);

        // Dictionary-specific should be untouched
        var all = await _repository.GetByVariableIdAsync(_testVariable.Id);
        var common = all.Where(r => r.DictionaryId == null).ToList();
        var dictSpecific = all.Where(r => r.DictionaryId == dictionary.Id).ToList();

        Assert.Single(common);
        Assert.Equal("New common", common[0].Meaning);
        Assert.Single(dictSpecific);
        Assert.Equal("Dictionary specific", dictSpecific[0].Meaning);
    }

    [Fact]
    public async Task SameVariableSameBit_DifferentDictionaries_BothPersist()
    {
        var dict1 = new DictionaryEntity { Name = "Dict1", IsStandard = false };
        var dict2 = new DictionaryEntity { Name = "Dict2", IsStandard = false };
        Context.Dictionaries.AddRange(dict1, dict2);
        await Context.SaveChangesAsync();

        // Common, Dict1, Dict2 — all word0 bit0 but different DictionaryId
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = null,
            WordIndex = 0, BitIndex = 0, Meaning = "Common"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = dict1.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Dict1 override"
        });
        await _repository.AddAsync(new BitInterpretationEntity
        {
            VariableId = _testVariable.Id, DictionaryId = dict2.Id,
            WordIndex = 0, BitIndex = 0, Meaning = "Dict2 override"
        });

        var all = await _repository.GetByVariableIdAsync(_testVariable.Id);

        Assert.Equal(3, all.Count);
        Assert.Contains(all, r => r.Meaning == "Common" && r.DictionaryId == null);
        Assert.Contains(all, r => r.Meaning == "Dict1 override" && r.DictionaryId == dict1.Id);
        Assert.Contains(all, r => r.Meaning == "Dict2 override" && r.DictionaryId == dict2.Id);
    }
}
