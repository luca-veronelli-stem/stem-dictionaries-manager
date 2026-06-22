using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Services.Validation;

namespace Tests.Integration.Services.Validation;

/// <summary>
/// Tests for <see cref="CommandValidator"/> in isolation from
/// <c>CommandService</c>, against real repositories on in-memory SQLite.
/// </summary>
public class CommandValidatorTests : IntegrationTestBase
{
    private readonly CommandValidator _validator;
    private readonly CommandRepository _repository;

    public CommandValidatorTests()
    {
        _repository = new CommandRepository(Context, NullLogger<RepositoryBase<CommandEntity>>.Instance);
        _validator = new CommandValidator(_repository);
    }

    [Fact]
    public async Task ValidateForCreateAsync_DuplicateName_ReturnsInvalid()
    {
        await _repository.AddAsync(new CommandEntity { Name = "READ", CodeHigh = 0x01, CodeLow = 0x00 });

        ValidationResult result = await _validator.ValidateForCreateAsync(
            new Command("READ", 0x09, 0x00));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task ValidateForCreateAsync_DuplicateCode_ReturnsInvalid()
    {
        await _repository.AddAsync(new CommandEntity
        {
            Name = "FIRST",
            CodeHigh = 0x02,
            CodeLow = 0x00,
            IsResponse = false
        });

        ValidationResult result = await _validator.ValidateForCreateAsync(
            new Command("SECOND", 0x02, 0x00, false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task ValidateForCreateAsync_UniqueNameAndCode_ReturnsValid()
    {
        ValidationResult result = await _validator.ValidateForCreateAsync(
            new Command("NEW", 0x0A, 0x00));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateForCreateAsync_SameCodeDifferentIsResponse_ReturnsValid()
    {
        await _repository.AddAsync(new CommandEntity
        {
            Name = "REQ",
            CodeHigh = 0x03,
            CodeLow = 0x00,
            IsResponse = false
        });

        ValidationResult result = await _validator.ValidateForCreateAsync(
            new Command("RSP", 0x03, 0x00, true));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateForUpdateAsync_RenameToExistingName_ReturnsInvalid()
    {
        await _repository.AddAsync(new CommandEntity { Name = "TAKEN", CodeHigh = 0x04, CodeLow = 0x00 });
        CommandEntity target = await _repository.AddAsync(
            new CommandEntity { Name = "TO_RENAME", CodeHigh = 0x05, CodeLow = 0x00 });

        ValidationResult result = await _validator.ValidateForUpdateAsync(
            Command.Restore(target.Id, "TAKEN", 0x05, 0x00, false, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }
}
