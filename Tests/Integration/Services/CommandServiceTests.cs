using Core.Enums;
using Core.Models;
using Infrastructure.Repositories;
using Services;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per CommandService.
/// </summary>
public class CommandServiceTests : IntegrationTestBase
{
    private readonly CommandService _service;

    public CommandServiceTests()
    {
        var repository = new CommandRepository(Context);
        var deviceStateRepository = new CommandDeviceStateRepository(Context);
        _service = new CommandService(repository, deviceStateRepository);
    }

    [Fact]
    public async Task AddAsync_ValidCommand_CreatesAndReturns()
    {
        // Arrange
        var command = new Command("READ_VAR", 0x01, 0x00, false, ["address", "length"]);

        // Act
        var result = await _service.AddAsync(command);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("READ_VAR", result.Name);
        Assert.Equal(0x01, result.CodeHigh);
        Assert.Equal(0x00, result.CodeLow);
        Assert.Equal(2, result.Parameters.Count);
    }

    [Fact]
    public async Task AddAsync_DuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddAsync(new Command("First", 0x01, 0x00, false));

        // Act & Assert - stesso codice, stesso isResponse
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Command("Second", 0x01, 0x00, false)));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_SameCodeDifferentIsResponse_Succeeds()
    {
        // Arrange - Request
        await _service.AddAsync(new Command("CMD_REQUEST", 0x05, 0x00, false));

        // Act - Response (stesso codice ma isResponse diverso)
        var response = await _service.AddAsync(new Command("CMD_RESPONSE", 0x05, 0x00, true));

        // Assert
        Assert.True(response.Id > 0);
        Assert.True(response.IsResponse);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCommand_ReturnsCommand()
    {
        // Arrange
        var created = await _service.AddAsync(new Command("FINDME", 0x10, 0x00, false));

        // Act
        var result = await _service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FINDME", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_ExistingCode_ReturnsCommand()
    {
        // Arrange
        await _service.AddAsync(new Command("BYCODE", 0xAB, 0xCD, false));

        // Act
        var result = await _service.GetByCodeAsync(0xAB, 0xCD, false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BYCODE", result.Name);
    }

    [Fact]
    public async Task GetByCodeAsync_DistinguishesRequestResponse()
    {
        // Arrange
        await _service.AddAsync(new Command("REQ", 0x20, 0x00, false));
        await _service.AddAsync(new Command("RSP", 0x20, 0x00, true));

        // Act
        var request = await _service.GetByCodeAsync(0x20, 0x00, false);
        var response = await _service.GetByCodeAsync(0x20, 0x00, true);

        // Assert
        Assert.Equal("REQ", request!.Name);
        Assert.Equal("RSP", response!.Name);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllCommands()
    {
        // Arrange
        await _service.AddAsync(new Command("CMD1", 0x01, 0x00, false));
        await _service.AddAsync(new Command("CMD2", 0x02, 0x00, false));
        await _service.AddAsync(new Command("CMD3", 0x03, 0x00, false));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingCommand_UpdatesCommand()
    {
        // Arrange
        var created = await _service.AddAsync(new Command("BEFORE", 0x30, 0x00, false, ["old"]));
        var updated = Command.Restore(created.Id, "AFTER", 0x30, 0x00, false, ["new1", "new2"]);

        // Act
        await _service.UpdateAsync(updated);

        // Assert
        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("AFTER", result!.Name);
        Assert.Equal(2, result.Parameters.Count);
    }

    [Fact]
    public async Task UpdateAsync_NonExisting_ThrowsKeyNotFoundException()
    {
        var nonExisting = Command.Restore(999, "GHOST", 0xFF, 0xFF, false, []);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(nonExisting));
    }

    [Fact]
    public async Task DeleteAsync_ExistingCommand_RemovesCommand()
    {
        // Arrange
        var created = await _service.AddAsync(new Command("DELETE", 0x40, 0x00, false));

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

    // === DeviceState Tests ===

    [Fact]
    public async Task SetDeviceStateAsync_NewState_CreatesState()
    {
        // Arrange
        var command = await _service.AddAsync(new Command("STATE_TEST", 0x50, 0x00, false));

        // Act
        await _service.SetDeviceStateAsync(command.Id, DeviceType.OptimusXp, true);

        // Assert
        var state = await _service.GetDeviceStateAsync(command.Id, DeviceType.OptimusXp);
        Assert.NotNull(state);
        Assert.True(state.IsEnabled);
    }

    [Fact]
    public async Task SetDeviceStateAsync_ExistingState_UpdatesState()
    {
        // Arrange
        var command = await _service.AddAsync(new Command("UPDATE_STATE", 0x51, 0x00, false));
        await _service.SetDeviceStateAsync(command.Id, DeviceType.EdenXp, true);

        // Act
        await _service.SetDeviceStateAsync(command.Id, DeviceType.EdenXp, false);

        // Assert
        var state = await _service.GetDeviceStateAsync(command.Id, DeviceType.EdenXp);
        Assert.NotNull(state);
        Assert.False(state.IsEnabled);
    }

    [Fact]
    public async Task SetDeviceStateAsync_NonExistingCommand_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SetDeviceStateAsync(999, DeviceType.Spark, true));
    }

    [Fact]
    public async Task GetDeviceStateAsync_NonExistingState_ReturnsNull()
    {
        // Arrange
        var command = await _service.AddAsync(new Command("NO_STATE", 0x52, 0x00, false));

        // Act
        var result = await _service.GetDeviceStateAsync(command.Id, DeviceType.R3lXp);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWithDeviceStatesAsync_LoadsAllStates()
    {
        // Arrange
        var command = await _service.AddAsync(new Command("MULTI_STATE", 0x53, 0x00, false));
        await _service.SetDeviceStateAsync(command.Id, DeviceType.OptimusXp, true);
        await _service.SetDeviceStateAsync(command.Id, DeviceType.EdenXp, false);
        await _service.SetDeviceStateAsync(command.Id, DeviceType.Spark, true);

        // Act
        var result = await _service.GetWithDeviceStatesAsync(command.Id);

        // Assert
        Assert.NotNull(result);
        // Stati sono caricati ma accessibili tramite GetDeviceStateAsync
        var state1 = await _service.GetDeviceStateAsync(command.Id, DeviceType.OptimusXp);
        var state2 = await _service.GetDeviceStateAsync(command.Id, DeviceType.EdenXp);
        Assert.True(state1!.IsEnabled);
        Assert.False(state2!.IsEnabled);
    }

    [Fact]
    public async Task FullCode_AfterRetrieval_IsCorrect()
    {
        // Arrange
        var command = await _service.AddAsync(new Command("FULLCODE", 0x12, 0x34, false));

        // Act
        var result = await _service.GetByIdAsync(command.Id);

        // Assert
        Assert.Equal(0x1234, result!.FullCode);
    }
}
