using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per DictionaryRepository e VariableRepository.
/// Testa anche le relazioni FK.
/// </summary>
public class DictionaryRepositoryTests : IntegrationTestBase
{
    private readonly DictionaryRepository _dictionaryRepo;
    private readonly VariableRepository _variableRepo;

    public DictionaryRepositoryTests()
    {
        _dictionaryRepo = new DictionaryRepository(Context);
        _variableRepo = new VariableRepository(Context);
    }

    [Fact]
    public async Task AddDictionary_WithBoardType_Works()
    {
        // Arrange
        var boardType = new BoardTypeEntity { Name = "Madre", FirmwareType = 17 };
        Context.BoardTypes.Add(boardType);
        await Context.SaveChangesAsync();

        var dictionary = new DictionaryEntity 
        { 
            Name = "optimus-xp", 
            BoardTypeId = boardType.Id 
        };

        // Act
        var result = await _dictionaryRepo.AddAsync(dictionary);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal(boardType.Id, result.BoardTypeId);
    }

    [Fact]
    public async Task GetStandardDictionary_ReturnsNullBoardType()
    {
        // Arrange - dizionario Standard non ha BoardType
        var dictionary = new DictionaryEntity 
        { 
            Name = "standard", 
            BoardTypeId = null 
        };
        await _dictionaryRepo.AddAsync(dictionary);

        // Act
        var result = await _dictionaryRepo.GetStandardDictionaryAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.BoardTypeId);
    }

    [Fact]
    public async Task GetWithVariables_IncludesVariables()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "test-dict" };
        await _dictionaryRepo.AddAsync(dictionary);

        var variable = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "Firmware",
            AddressHigh = 0x00,
            AddressLow = 0x00,
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "uint16_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        // Act
        var result = await _dictionaryRepo.GetWithVariablesAsync(dictionary.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Variables);
        Assert.Equal("Firmware", result.Variables.First().Name);
    }

    [Fact]
    public async Task Variable_UniqueAddressPerDictionary_Enforced()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "test" };
        await _dictionaryRepo.AddAsync(dictionary);

        var var1 = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "Var1",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(var1);

        var var2 = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "Var2",
            AddressHigh = 0x00,
            AddressLow = 0x01, // Stesso indirizzo!
            DataTypeKind = DataTypeKind.UInt16,
            DataTypeRaw = "uint16_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };

        // Act & Assert - deve fallire per unique constraint
        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => _variableRepo.AddAsync(var2));
    }

    [Fact]
    public async Task GetByAddressAsync_FindsVariable()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "test" };
        await _dictionaryRepo.AddAsync(dictionary);

        var variable = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "Target",
            AddressHigh = 0x80,
            AddressLow = 0x15,
            DataTypeKind = DataTypeKind.Float,
            DataTypeRaw = "float",
            AccessMode = AccessMode.ReadWrite,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        // Act
        var result = await _variableRepo.GetByAddressAsync(dictionary.Id, 0x80, 0x15);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Target", result.Name);
    }

    // === Test per metodi aggiunti con SVC-001 ===

    [Fact]
    public async Task GetAllWithBoardTypeAsync_ReturnsAllWithBoardType()
    {
        // Arrange
        var boardType = new BoardTypeEntity { Name = "TestBoard", FirmwareType = 99 };
        Context.BoardTypes.Add(boardType);
        await Context.SaveChangesAsync();

        await _dictionaryRepo.AddAsync(new DictionaryEntity 
        { 
            Name = "dict-with-board", 
            BoardTypeId = boardType.Id 
        });
        await _dictionaryRepo.AddAsync(new DictionaryEntity 
        { 
            Name = "dict-standard", 
            BoardTypeId = null 
        });

        // Act
        var result = await _dictionaryRepo.GetAllWithBoardTypeAsync();

        // Assert
        Assert.Equal(2, result.Count);
        var withBoard = result.First(d => d.Name == "dict-with-board");
        Assert.NotNull(withBoard.BoardType);
        Assert.Equal("TestBoard", withBoard.BoardType!.Name);
    }

    [Fact]
    public async Task GetAllWithBoardTypeAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await _dictionaryRepo.GetAllWithBoardTypeAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        var dictionary = new DictionaryEntity { Name = "exists-test" };
        await _dictionaryRepo.AddAsync(dictionary);

        var result = await _dictionaryRepo.ExistsAsync(dictionary.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _dictionaryRepo.ExistsAsync(999);

        Assert.False(result);
    }

    // === Test per VariableRepository metodi aggiunti con SVC-001 ===

    [Fact]
    public async Task Variable_ExistsAsync_ExistingId_ReturnsTrue()
    {
        var dictionary = new DictionaryEntity { Name = "var-exists" };
        await _dictionaryRepo.AddAsync(dictionary);

        var variable = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "TestVar",
            AddressHigh = 0x00,
            AddressLow = 0x50,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        var result = await _variableRepo.ExistsAsync(variable.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task Variable_ExistsAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _variableRepo.ExistsAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task GetWithBitInterpretationsAsync_ReturnsWithInterpretations()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "bit-interp-test" };
        await _dictionaryRepo.AddAsync(dictionary);

        var variable = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "BitmappedVar",
            AddressHigh = 0x00,
            AddressLow = 0x60,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[1]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        Context.BitInterpretations.Add(new BitInterpretationEntity
        {
            VariableId = variable.Id,
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Bit 0"
        });
        Context.BitInterpretations.Add(new BitInterpretationEntity
        {
            VariableId = variable.Id,
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Bit 1"
        });
        await Context.SaveChangesAsync();

        // Act
        var result = await _variableRepo.GetWithBitInterpretationsAsync(variable.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.BitInterpretations.Count);
    }

    [Fact]
    public async Task GetWithBitInterpretationsAsync_NotFound_ReturnsNull()
    {
        var result = await _variableRepo.GetWithBitInterpretationsAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetWithBitInterpretationsAsync_NoInterpretations_ReturnsEmptyList()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "no-interp" };
        await _dictionaryRepo.AddAsync(dictionary);

        var variable = new VariableEntity
        {
            DictionaryId = dictionary.Id,
            Name = "NoInterpVar",
            AddressHigh = 0x00,
            AddressLow = 0x70,
            DataTypeKind = DataTypeKind.Bitmapped,
            DataTypeRaw = "bitmapped[1]",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        };
        await _variableRepo.AddAsync(variable);

        // Act
        var result = await _variableRepo.GetWithBitInterpretationsAsync(variable.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.BitInterpretations);
    }
}
