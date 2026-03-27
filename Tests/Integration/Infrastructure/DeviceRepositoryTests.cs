using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per DeviceRepository.
/// </summary>
public class DeviceRepositoryTests : IntegrationTestBase
{
    private readonly DeviceRepository _repository;

    public DeviceRepositoryTests()
    {
        _repository = new DeviceRepository(Context);
    }

    [Fact]
    public async Task AddAsync_CreatesDevice()
    {
        var entity = new DeviceEntity
        {
            Name = "Eden-XP",
            MachineCode = 3,
            Description = "Supporto barella"
        };

        var result = await _repository.AddAsync(entity);

        Assert.True(result.Id > 0);
        Assert.Equal("Eden-XP", result.Name);
        Assert.Equal(3, result.MachineCode);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDevice()
    {
        var entity = new DeviceEntity { Name = "Spark", MachineCode = 7 };
        await _repository.AddAsync(entity);

        var result = await _repository.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal("Spark", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAll()
    {
        await _repository.AddAsync(new DeviceEntity { Name = "A", MachineCode = 1 });
        await _repository.AddAsync(new DeviceEntity { Name = "B", MachineCode = 2 });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByNameAsync_Found_ReturnsDevice()
    {
        await _repository.AddAsync(new DeviceEntity
        {
            Name = "Optimus-XP",
            MachineCode = 10
        });

        var result = await _repository.GetByNameAsync("Optimus-XP");

        Assert.NotNull(result);
        Assert.Equal(10, result.MachineCode);
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByNameAsync("NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByMachineCodeAsync_Found_ReturnsDevice()
    {
        await _repository.AddAsync(new DeviceEntity
        {
            Name = "R3L-XP",
            MachineCode = 11
        });

        var result = await _repository.GetByMachineCodeAsync(11);

        Assert.NotNull(result);
        Assert.Equal("R3L-XP", result.Name);
    }

    [Fact]
    public async Task GetByMachineCodeAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByMachineCodeAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesDevice()
    {
        var entity = await _repository.AddAsync(
            new DeviceEntity { Name = "ToDelete", MachineCode = 99 });

        await _repository.DeleteAsync(entity.Id);

        Assert.Null(await _repository.GetByIdAsync(entity.Id));
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDevice()
    {
        var entity = await _repository.AddAsync(
            new DeviceEntity { Name = "Before", MachineCode = 50 });

        entity.Name = "After";
        entity.MachineCode = 51;
        await _repository.UpdateAsync(entity);

        var result = await _repository.GetByIdAsync(entity.Id);
        Assert.Equal("After", result!.Name);
        Assert.Equal(51, result.MachineCode);
    }
}
