using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Services;
using Services.Interfaces;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per DictionaryService (Domain v2).
/// </summary>
public class DictionaryServiceTests : IntegrationTestBase
{
    private readonly DictionaryService _service;

    public DictionaryServiceTests()
    {
        SeedTestUser();
        var dictionaryRepository = new DictionaryRepository(Context, NullLogger<RepositoryBase<DictionaryEntity>>.Instance);
        var variableRepository = new VariableRepository(Context, NullLogger<RepositoryBase<VariableEntity>>.Instance);
        var auditRepository = new AuditEntryRepository(Context, NullLogger<RepositoryBase<AuditEntryEntity>>.Instance);
        IAuditService auditService = new AuditService(auditRepository, NullLogger<AuditService>.Instance);
        ICurrentUserProvider userProvider = new CurrentUserProvider { CurrentUserId = 1 };
        _service = new DictionaryService(
            dictionaryRepository, variableRepository, auditService, userProvider, NullLogger<DictionaryService>.Instance);
    }

    [Fact]
    public async Task AddAsync_ValidDictionary_CreatesAndReturnsDictionary()
    {
        var dictionary = new Core.Models.Dictionary("test-dict", "Test Description");

        Dictionary result = await _service.AddAsync(dictionary);

        Assert.True(result.Id > 0);
        Assert.Equal("test-dict", result.Name);
        Assert.Equal("Test Description", result.Description);
        Assert.False(result.IsStandard);
    }

    [Fact]
    public async Task AddAsync_StandardDictionary_SetsIsStandard()
    {
        var dictionary = new Core.Models.Dictionary("standard", isStandard: true);

        Dictionary result = await _service.AddAsync(dictionary);

        Assert.True(result.IsStandard);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Core.Models.Dictionary("duplicate"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Core.Models.Dictionary("duplicate")));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_SecondStandardDictionary_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Core.Models.Dictionary("standard", isStandard: true));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Core.Models.Dictionary("standard-2", isStandard: true)));
        Assert.Contains("Standard dictionary", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_MultipleNonStandard_IsAllowed()
    {
        await _service.AddAsync(new Core.Models.Dictionary("dict1"));
        Dictionary second = await _service.AddAsync(new Core.Models.Dictionary("dict2"));

        Assert.NotNull(second);
        Assert.False(second.IsStandard);
    }

    [Fact]
    public async Task UpdateAsync_ChangeToStandard_WhenOneExists_ThrowsInvalidOperationException()
    {
        await _service.AddAsync(new Core.Models.Dictionary("standard", isStandard: true));
        Dictionary nonStandard = await _service.AddAsync(new Core.Models.Dictionary("non-standard"));

        var updated = Core.Models.Dictionary.Restore(
            nonStandard.Id, "non-standard", null, true, []);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(updated));
        Assert.Contains("Standard dictionary", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingDictionary_ReturnsDictionary()
    {
        Dictionary created = await _service.AddAsync(new Core.Models.Dictionary("findme"));

        Dictionary? result = await _service.GetByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal("findme", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        Assert.Null(await _service.GetByIdAsync(999));
    }

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsDictionary()
    {
        await _service.AddAsync(new Core.Models.Dictionary("byname"));

        Dictionary? result = await _service.GetByNameAsync("byname");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllDictionaries()
    {
        await _service.AddAsync(new Core.Models.Dictionary("dict1"));
        await _service.AddAsync(new Core.Models.Dictionary("dict2"));

        IReadOnlyList<Dictionary> result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetWithVariablesAsync_ReturnsVariables()
    {
        Dictionary created = await _service.AddAsync(new Core.Models.Dictionary("with-vars"));
        await _service.AddVariableAsync(created.Id,
            new Variable("Var1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));
        await _service.AddVariableAsync(created.Id,
            new Variable("Var2", 0x00, 0x02, DataTypeKind.UInt16, AccessMode.ReadWrite, "uint16_t"));

        Dictionary? result = await _service.GetWithVariablesAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(2, result.Variables.Count);
    }

    [Fact]
    public async Task GetStandardDictionaryAsync_WhenExists_ReturnsStandard()
    {
        await _service.AddAsync(new Core.Models.Dictionary("standard", "Standard vars", true));

        Dictionary? result = await _service.GetStandardDictionaryAsync();

        Assert.NotNull(result);
        Assert.True(result.IsStandard);
    }

    [Fact]
    public async Task AddVariableAsync_ValidVariable_AddsToDict()
    {
        Dictionary dict = await _service.AddAsync(new Core.Models.Dictionary("add-var"));
        var variable = new Variable("NewVar", 0x00, 0x10, DataTypeKind.UInt32,
            AccessMode.ReadWrite, "uint32_t");

        Variable result = await _service.AddVariableAsync(dict.Id, variable);

        Assert.True(result.Id > 0);
        Assert.Equal("NewVar", result.Name);
    }

    [Fact]
    public async Task AddVariableAsync_DuplicateAddress_ThrowsInvalidOperationException()
    {
        Dictionary dict = await _service.AddAsync(new Core.Models.Dictionary("dup-addr"));
        await _service.AddVariableAsync(dict.Id,
            new Variable("First", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
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
        Dictionary dict = await _service.AddAsync(new Core.Models.Dictionary("remove-var"));
        Variable variable = await _service.AddVariableAsync(dict.Id,
            new Variable("ToRemove", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));

        await _service.RemoveVariableAsync(dict.Id, variable.Id);

        Dictionary? result = await _service.GetWithVariablesAsync(dict.Id);
        Assert.Empty(result!.Variables);
    }

    [Fact]
    public async Task RemoveVariableAsync_VariableNotInDict_ThrowsInvalidOperationException()
    {
        Dictionary dict1 = await _service.AddAsync(new Core.Models.Dictionary("dict1"));
        Dictionary dict2 = await _service.AddAsync(new Core.Models.Dictionary("dict2"));
        Variable variable = await _service.AddVariableAsync(dict1.Id,
            new Variable("InDict1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RemoveVariableAsync(dict2.Id, variable.Id));
        Assert.Contains("does not belong", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ExistingDictionary_UpdatesDictionary()
    {
        Dictionary created = await _service.AddAsync(new Core.Models.Dictionary("update-me", "Before"));
        var updated = Core.Models.Dictionary.Restore(created.Id, "update-me", "After", false, []);

        await _service.UpdateAsync(updated);

        Dictionary? result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("After", result!.Description);
    }

    [Fact]
    public async Task DeleteAsync_ExistingDictionary_RemovesDictionary()
    {
        Dictionary created = await _service.AddAsync(new Core.Models.Dictionary("delete-me"));

        await _service.DeleteAsync(created.Id);

        Assert.Null(await _service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(999));
    }
}
