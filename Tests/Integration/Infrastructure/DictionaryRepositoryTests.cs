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
}
