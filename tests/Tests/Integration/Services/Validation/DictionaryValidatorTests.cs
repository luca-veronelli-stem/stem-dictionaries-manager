using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services.Validation;

namespace Tests.Integration.Services.Validation;

/// <summary>
/// Tests for <see cref="DictionaryValidator"/> in isolation from
/// <c>DictionaryService</c>, against real repositories on in-memory SQLite.
/// </summary>
public class DictionaryValidatorTests : IntegrationTestBase
{
    private readonly DictionaryValidator _validator;
    private readonly DictionaryRepository _repository;

    public DictionaryValidatorTests()
    {
        _repository = new DictionaryRepository(Context);
        _validator = new DictionaryValidator(_repository);
    }

    [Fact]
    public async Task ValidateForCreateAsync_DuplicateName_ReturnsInvalid()
    {
        await _repository.AddAsync(new DictionaryEntity { Name = "existing" });

        ValidationResult result = await _validator.ValidateForCreateAsync(
            new Core.Models.Dictionary("existing"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task ValidateForCreateAsync_UniqueName_ReturnsValid()
    {
        ValidationResult result = await _validator.ValidateForCreateAsync(
            new Core.Models.Dictionary("brand-new"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateForCreateAsync_SecondStandardDictionary_ReturnsInvalid()
    {
        await _repository.AddAsync(new DictionaryEntity { Name = "std", IsStandard = true });

        ValidationResult result = await _validator.ValidateForCreateAsync(
            new Core.Models.Dictionary("std-2", isStandard: true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Standard dictionary"));
    }

    [Fact]
    public async Task ValidateForUpdateAsync_RenameToExistingName_ReturnsInvalid()
    {
        await _repository.AddAsync(new DictionaryEntity { Name = "taken" });
        DictionaryEntity target = await _repository.AddAsync(new DictionaryEntity { Name = "to-rename" });

        ValidationResult result = await _validator.ValidateForUpdateAsync(
            Core.Models.Dictionary.Restore(target.Id, "taken", null, false, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task ValidateForUpdateAsync_UnchangedName_ReturnsValid()
    {
        DictionaryEntity target = await _repository.AddAsync(new DictionaryEntity { Name = "keep" });

        ValidationResult result = await _validator.ValidateForUpdateAsync(
            Core.Models.Dictionary.Restore(target.Id, "keep", null, false, []));

        Assert.True(result.IsValid);
    }
}
