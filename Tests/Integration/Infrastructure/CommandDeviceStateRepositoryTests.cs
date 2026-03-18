using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per CommandDeviceStateRepository.
/// </summary>
public class CommandDeviceStateRepositoryTests : IntegrationTestBase
{
    private readonly CommandDeviceStateRepository _repository;
    private readonly CommandRepository _commandRepo;
    private CommandEntity _testCommand = null!;

    public CommandDeviceStateRepositoryTests()
    {
        _repository = new CommandDeviceStateRepository(Context);
        _commandRepo = new CommandRepository(Context);
    }

    public override async Task InitializeAsync()
    {
        _testCommand = new CommandEntity
        {
            Name = "TEST_COMMAND",
            CodeHigh = 0x01,
            CodeLow = 0x00,
            IsResponse = false
        };
        await _commandRepo.AddAsync(_testCommand);
    }

    [Fact]
    public async Task AddAsync_CreatesState()
    {
        var state = new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.OptimusXp,
            IsEnabled = true
        };

        var result = await _repository.AddAsync(state);

        Assert.True(result.Id > 0);
        Assert.Equal(DeviceType.OptimusXp, result.DeviceType);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsState()
    {
        var state = new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.Eden,
            IsEnabled = false
        };
        await _repository.AddAsync(state);

        var result = await _repository.GetByIdAsync(state.Id);

        Assert.NotNull(result);
        Assert.Equal(DeviceType.Eden, result.DeviceType);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCommandAndDeviceAsync_ReturnsState()
    {
        await _repository.AddAsync(new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.Spark,
            IsEnabled = true
        });

        var result = await _repository.GetByCommandAndDeviceAsync(_testCommand.Id, DeviceType.Spark);

        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
    }

    [Fact]
    public async Task GetByCommandAndDeviceAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByCommandAndDeviceAsync(_testCommand.Id, DeviceType.BleModule);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCommandIdAsync_ReturnsAllStates()
    {
        // Arrange - aggiungi stati per diversi device
        await _repository.AddAsync(new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.OptimusXp,
            IsEnabled = true
        });
        await _repository.AddAsync(new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.Eden,
            IsEnabled = false
        });
        await _repository.AddAsync(new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.Spark,
            IsEnabled = true
        });

        // Act
        var result = await _repository.GetByCommandIdAsync(_testCommand.Id);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetByCommandIdAsync_NoStates_ReturnsEmptyList()
    {
        var result = await _repository.GetByCommandIdAsync(999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesState()
    {
        var state = new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.Gradino,
            IsEnabled = false
        };
        await _repository.AddAsync(state);

        state.IsEnabled = true;
        await _repository.UpdateAsync(state);

        var result = await _repository.GetByIdAsync(state.Id);
        Assert.True(result!.IsEnabled);
    }

    [Fact]
    public async Task DeleteAsync_RemovesState()
    {
        var state = new CommandDeviceStateEntity
        {
            CommandId = _testCommand.Id,
            DeviceType = DeviceType.Spyke,
            IsEnabled = true
        };
        await _repository.AddAsync(state);

        await _repository.DeleteAsync(state.Id);

        var result = await _repository.GetByIdAsync(state.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }
}
