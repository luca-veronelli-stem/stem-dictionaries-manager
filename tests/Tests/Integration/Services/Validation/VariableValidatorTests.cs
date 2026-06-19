using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services.Validation;

namespace Tests.Integration.Services.Validation;

/// <summary>
/// Tests for <see cref="VariableValidator"/> in isolation from
/// <c>VariableService</c>, against real repositories on in-memory SQLite.
/// </summary>
public class VariableValidatorTests : IntegrationTestBase
{
    private readonly VariableValidator _validator;
    private readonly VariableRepository _repository;
    private int _dictionaryId;

    public VariableValidatorTests()
    {
        _repository = new VariableRepository(Context);
        _validator = new VariableValidator(_repository);
    }

    public override async Task InitializeAsync()
    {
        var dict = new DictionaryEntity { Name = "dict" };
        Context.Dictionaries.Add(dict);
        await Context.SaveChangesAsync();
        _dictionaryId = dict.Id;
    }

    private async Task SeedVariableAsync(byte addressHigh, byte addressLow, string name) =>
        await _repository.AddAsync(new VariableEntity
        {
            DictionaryId = _dictionaryId,
            Name = name,
            AddressHigh = addressHigh,
            AddressLow = addressLow,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        });

    private static Variable MakeVariable(byte addressHigh, byte addressLow, string name = "Var") =>
        new(name, addressHigh, addressLow, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t");

    [Fact]
    public async Task ValidateForCreateAsync_DuplicateAddress_ReturnsInvalid()
    {
        await SeedVariableAsync(0x00, 0x01, "First");

        ValidationResult result = await _validator.ValidateForCreateAsync(
            _dictionaryId, MakeVariable(0x00, 0x01, "Second"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task ValidateForCreateAsync_UniqueAddress_ReturnsValid()
    {
        ValidationResult result = await _validator.ValidateForCreateAsync(
            _dictionaryId, MakeVariable(0x00, 0x05));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateForUpdateAsync_MoveToTakenAddress_ReturnsInvalid()
    {
        await SeedVariableAsync(0x00, 0x01, "A");
        VariableEntity moving = await _repository.AddAsync(new VariableEntity
        {
            DictionaryId = _dictionaryId,
            Name = "B",
            AddressHigh = 0x00,
            AddressLow = 0x02,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        });

        var update = Variable.Restore(moving.Id, "B", 0x00, 0x01, DataTypeKind.UInt8,
            "uint8_t", null, AccessMode.ReadOnly, true, null, null, null, null, null, null);

        ValidationResult result = await _validator.ValidateForUpdateAsync(update);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task ValidateForUpdateAsync_UnchangedAddress_ReturnsValid()
    {
        VariableEntity existing = await _repository.AddAsync(new VariableEntity
        {
            DictionaryId = _dictionaryId,
            Name = "Keep",
            AddressHigh = 0x00,
            AddressLow = 0x07,
            DataTypeKind = DataTypeKind.UInt8,
            DataTypeRaw = "uint8_t",
            AccessMode = AccessMode.ReadOnly,
            IsEnabled = true
        });

        var update = Variable.Restore(existing.Id, "Keep", 0x00, 0x07, DataTypeKind.UInt8,
            "uint8_t", null, AccessMode.ReadOnly, true, null, null, null, null, null, null);

        ValidationResult result = await _validator.ValidateForUpdateAsync(update);

        Assert.True(result.IsValid);
    }
}
