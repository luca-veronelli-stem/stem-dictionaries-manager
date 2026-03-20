using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per DictionaryService (aggregate root).
/// </summary>
public class DictionaryServiceTests : IntegrationTestBase
{
    private readonly DictionaryService _service;
    private BoardTypeEntity _testBoardType = null!;

    public DictionaryServiceTests()
    {
        var dictionaryRepository = new DictionaryRepository(Context);
        var variableRepository = new VariableRepository(Context);
        var boardTypeRepository = new BoardTypeRepository(Context);

        _service = new DictionaryService(
            dictionaryRepository, 
            variableRepository, 
            boardTypeRepository);
    }

    public override async Task InitializeAsync()
    {
        _testBoardType = new BoardTypeEntity { Name = "TestBoard", FirmwareType = 99 };
        Context.BoardTypes.Add(_testBoardType);
        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAsync_ValidDictionary_CreatesAndReturnsDictionary()
    {
        // Arrange
        var dictionary = new Core.Models.Dictionary("test-dict", description: "Test Description");

        // Act
        var result = await _service.AddAsync(dictionary);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("test-dict", result.Name);
        Assert.Equal("Test Description", result.Description);
    }

    [Fact]
    public async Task AddAsync_WithBoardType_AssociatesBoardType()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var dictionary = new Core.Models.Dictionary("with-boardtype", DeviceType.Optimus, boardType, "Has BoardType");

        // Act
        var result = await _service.AddAsync(dictionary);

        // Assert
        Assert.NotNull(result.BoardType);
        Assert.Equal(_testBoardType.Name, result.BoardType!.Name);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddAsync(new Core.Models.Dictionary("duplicate"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Core.Models.Dictionary("duplicate")));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_BoardTypeAlreadyHasDictionary_ThrowsInvalidOperationException()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Core.Models.Dictionary("first", DeviceType.Optimus, boardType));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Core.Models.Dictionary("second", DeviceType.Optimus, boardType)));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_SameBoardType_DifferentDeviceType_Succeeds()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Core.Models.Dictionary("first", DeviceType.Optimus, boardType));

        // Act - Stesso BoardType ma DeviceType diverso → OK
        var second = await _service.AddAsync(new Core.Models.Dictionary("second", DeviceType.Eden, boardType));

        // Assert
        Assert.NotNull(second);
        Assert.Equal(DeviceType.Eden, second.DeviceType);
    }

    [Fact]
    public async Task AddAsync_SecondStandardDictionary_ThrowsInvalidOperationException()
    {
        // Arrange - Crea il primo dizionario Standard (senza BoardType)
        await _service.AddAsync(new Core.Models.Dictionary("standard", description: "First standard"));

        // Act & Assert - Il secondo senza BoardType deve fallire
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Core.Models.Dictionary("standard-2", description: "Second standard")));
        Assert.Contains("Standard dictionary", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ChangingToStandard_WhenOneExists_ThrowsInvalidOperationException()
    {
        // Arrange - Crea dizionario Standard e uno con BoardType
        await _service.AddAsync(new Core.Models.Dictionary("standard"));
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var withBoardType = await _service.AddAsync(new Core.Models.Dictionary("with-bt", DeviceType.Optimus, boardType));

        // Act & Assert - Cambiare a Standard (null BoardType) deve fallire
        var updated = Core.Models.Dictionary.Restore(
            withBoardType.Id, "with-bt", null, null, null, []);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(updated));
        Assert.Contains("Standard dictionary", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingDictionary_ReturnsDictionary()
    {
        // Arrange
        var created = await _service.AddAsync(new Core.Models.Dictionary("findme"));

        // Act
        var result = await _service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("findme", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsDictionary()
    {
        // Arrange
        await _service.AddAsync(new Core.Models.Dictionary("byname"));

        // Act
        var result = await _service.GetByNameAsync("byname");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDictionaries()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        await _service.AddAsync(new Core.Models.Dictionary("dict1"));
        await _service.AddAsync(new Core.Models.Dictionary("dict2", DeviceType.Optimus, boardType));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetWithVariablesAsync_ReturnsVariables()
    {
        // Arrange
        var created = await _service.AddAsync(new Core.Models.Dictionary("with-vars"));
        
        var var1 = new Variable("Var1", 0x00, 0x01, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t");
        var var2 = new Variable("Var2", 0x00, 0x02, DataTypeKind.UInt16, 
            AccessMode.ReadWrite, "uint16_t");
        
        await _service.AddVariableAsync(created.Id, var1);
        await _service.AddVariableAsync(created.Id, var2);

        // Act
        var result = await _service.GetWithVariablesAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Variables.Count);
        Assert.Contains(result.Variables, v => v.Name == "Var1");
        Assert.Contains(result.Variables, v => v.Name == "Var2");
    }

    [Fact]
    public async Task GetStandardDictionaryAsync_WhenExists_ReturnsStandard()
    {
        // Arrange - Standard = no BoardType
        await _service.AddAsync(new Core.Models.Dictionary("standard", description: "Standard vars"));

        // Act
        var result = await _service.GetStandardDictionaryAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.BoardType);
    }

    [Fact]
    public async Task AddVariableAsync_ValidVariable_AddsToDict()
    {
        // Arrange
        var dict = await _service.AddAsync(new Core.Models.Dictionary("add-var"));
        var variable = new Variable("NewVar", 0x00, 0x10, DataTypeKind.UInt32, 
            AccessMode.ReadWrite, "uint32_t");

        // Act
        var result = await _service.AddVariableAsync(dict.Id, variable);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("NewVar", result.Name);
    }

    [Fact]
    public async Task AddVariableAsync_DuplicateAddress_ThrowsInvalidOperationException()
    {
        // Arrange
        var dict = await _service.AddAsync(new Core.Models.Dictionary("dup-addr"));
        await _service.AddVariableAsync(dict.Id, 
            new Variable("First", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddVariableAsync(dict.Id, 
                new Variable("Second", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadOnly, "uint16_t")));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddVariableAsync_NonExistingDictionary_ThrowsKeyNotFoundException()
    {
        var variable = new Variable("Test", 0x00, 0x01, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, "uint8_t");

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.AddVariableAsync(999, variable));
    }

    [Fact]
    public async Task RemoveVariableAsync_ExistingVariable_RemovesFromDict()
    {
        // Arrange
        var dict = await _service.AddAsync(new Core.Models.Dictionary("remove-var"));
        var variable = await _service.AddVariableAsync(dict.Id, 
            new Variable("ToRemove", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));

        // Act
        await _service.RemoveVariableAsync(dict.Id, variable.Id);

        // Assert
        var result = await _service.GetWithVariablesAsync(dict.Id);
        Assert.Empty(result!.Variables);
    }

    [Fact]
    public async Task RemoveVariableAsync_VariableNotInDict_ThrowsInvalidOperationException()
    {
        // Arrange
        var boardType = BoardType.Restore(_testBoardType.Id, _testBoardType.Name, _testBoardType.FirmwareType);
        var dict1 = await _service.AddAsync(new Core.Models.Dictionary("dict1"));
        var dict2 = await _service.AddAsync(new Core.Models.Dictionary("dict2", DeviceType.Optimus, boardType));
        var variable = await _service.AddVariableAsync(dict1.Id, 
            new Variable("InDict1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RemoveVariableAsync(dict2.Id, variable.Id));
        Assert.Contains("does not belong", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ExistingDictionary_UpdatesDictionary()
    {
        // Arrange
        var created = await _service.AddAsync(new Core.Models.Dictionary("update-me", description: "Before"));
        var updated = Core.Models.Dictionary.Restore(created.Id, "update-me", null, null, "After", []);

        // Act
        await _service.UpdateAsync(updated);

        // Assert
        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("After", result!.Description);
    }

    [Fact]
    public async Task DeleteAsync_ExistingDictionary_RemovesDictionary()
    {
        // Arrange
        var created = await _service.AddAsync(new Core.Models.Dictionary("delete-me"));

        // Act
        await _service.DeleteAsync(created.Id);

        // Assert
        var result = await _service.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(999));
    }
}
