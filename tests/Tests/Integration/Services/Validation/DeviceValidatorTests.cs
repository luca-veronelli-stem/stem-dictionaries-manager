using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Services.Validation;

namespace Tests.Integration.Services.Validation;

/// <summary>
/// Tests for <see cref="DeviceValidator"/> in isolation from
/// <c>DeviceService</c>, against real repositories on in-memory SQLite.
/// </summary>
public class DeviceValidatorTests : IntegrationTestBase
{
    private readonly DeviceValidator _validator;
    private readonly DeviceRepository _repository;

    public DeviceValidatorTests()
    {
        _repository = new DeviceRepository(Context);
        _validator = new DeviceValidator(_repository);
    }

    [Fact]
    public async Task ValidateForCreateAsync_DuplicateName_ReturnsInvalid()
    {
        await _repository.AddAsync(new DeviceEntity { Name = "Eden-XP", MachineCode = 3 });

        ValidationResult result = await _validator.ValidateForCreateAsync(new Device("Eden-XP", 4));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }

    [Fact]
    public async Task ValidateForCreateAsync_DuplicateMachineCode_ReturnsInvalid()
    {
        await _repository.AddAsync(new DeviceEntity { Name = "Sherpa", MachineCode = 5 });

        ValidationResult result = await _validator.ValidateForCreateAsync(new Device("Spyke", 5));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("MachineCode"));
    }

    [Fact]
    public async Task ValidateForCreateAsync_UniqueNameAndMachineCode_ReturnsValid()
    {
        ValidationResult result = await _validator.ValidateForCreateAsync(new Device("Brand-New", 12));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateForUpdateAsync_ExcludesItself_ReturnsValid()
    {
        DeviceEntity device = await _repository.AddAsync(new DeviceEntity { Name = "Keep", MachineCode = 8 });

        ValidationResult result = await _validator.ValidateForUpdateAsync(
            Device.Restore(device.Id, "Keep", 8, null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateForUpdateAsync_RenameToExistingName_ReturnsInvalid()
    {
        await _repository.AddAsync(new DeviceEntity { Name = "Taken", MachineCode = 9 });
        DeviceEntity target = await _repository.AddAsync(new DeviceEntity { Name = "ToRename", MachineCode = 10 });

        ValidationResult result = await _validator.ValidateForUpdateAsync(
            Device.Restore(target.Id, "Taken", 10, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("already exists"));
    }
}
