using Core.Models;
using Infrastructure.Repositories;
using Services;
using Services.Interfaces;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per CommandService.
/// </summary>
public class CommandServiceTests : IntegrationTestBase
{
    private readonly CommandService _service;

    public CommandServiceTests()
    {
        SeedTestUser();
        var repository = new CommandRepository(Context);
        var deviceStateRepository = new CommandDeviceStateRepository(Context);
        var auditRepository = new AuditEntryRepository(Context);
        IAuditService auditService = new AuditService(auditRepository);
        ICurrentUserProvider userProvider = new CurrentUserProvider { CurrentUserId = 1 };
        _service = new CommandService(
            repository, deviceStateRepository, auditService, userProvider);
    }

    [Fact]
    public async Task AddAsync_ValidCommand_CreatesAndReturns()
    {
        // Arrange
        var command = new Command("READ_VAR", 0x01, 0x00, false, ["address", "length"]);

        // Act
        Command result = await _service.AddAsync(command);

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
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Command("Second", 0x01, 0x00, false)));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddAsync(new Command("SAME_NAME", 0x01, 0x00, false));

        // Act & Assert - stesso nome, codice diverso
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new Command("SAME_NAME", 0x02, 0x00, false)));
        Assert.Contains("SAME_NAME", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task AddAsync_SameCodeDifferentIsResponse_Succeeds()
    {
        // Arrange - Request
        await _service.AddAsync(new Command("CMD_REQUEST", 0x05, 0x00, false));

        // Act - Response (stesso codice ma isResponse diverso)
        Command response = await _service.AddAsync(new Command("CMD_RESPONSE", 0x05, 0x00, true));

        // Assert
        Assert.True(response.Id > 0);
        Assert.True(response.IsResponse);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCommand_ReturnsCommand()
    {
        // Arrange
        Command created = await _service.AddAsync(new Command("FINDME", 0x10, 0x00, false));

        // Act
        Command? result = await _service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FINDME", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExisting_ReturnsNull()
    {
        Command? result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_ExistingCode_ReturnsCommand()
    {
        // Arrange
        await _service.AddAsync(new Command("BYCODE", 0xAB, 0xCD, false));

        // Act
        Command? result = await _service.GetByCodeAsync(0xAB, 0xCD, false);

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
        Command? request = await _service.GetByCodeAsync(0x20, 0x00, false);
        Command? response = await _service.GetByCodeAsync(0x20, 0x00, true);

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
        IReadOnlyList<Command> result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingCommand_UpdatesCommand()
    {
        // Arrange
        Command created = await _service.AddAsync(new Command("BEFORE", 0x30, 0x00, false, ["old"]));
        var updated = Command.Restore(created.Id, "AFTER", 0x30, 0x00, false, ["new1", "new2"]);

        // Act
        await _service.UpdateAsync(updated);

        // Assert
        Command? result = await _service.GetByIdAsync(created.Id);
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
    public async Task UpdateAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddAsync(new Command("EXISTING", 0x01, 0x00, false));
        Command toUpdate = await _service.AddAsync(new Command("TOUPDATE", 0x02, 0x00, false));

        // Act - provo a rinominare con nome già esistente
        var renamed = Command.Restore(toUpdate.Id, "EXISTING", 0x02, 0x00, false, []);

        // Assert
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(renamed));
        Assert.Contains("EXISTING", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_SameName_Succeeds()
    {
        // Arrange - creo comando
        Command created = await _service.AddAsync(new Command("KEEPNAME", 0x70, 0x00, false));

        // Act - aggiorno altro campo, stesso nome
        var updated = Command.Restore(created.Id, "KEEPNAME", 0x70, 0x00, false, ["newparam"]);
        await _service.UpdateAsync(updated);

        // Assert
        Command? result = await _service.GetByIdAsync(created.Id);
        Assert.Single(result!.Parameters);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCommand_RemovesCommand()
    {
        // Arrange
        Command created = await _service.AddAsync(new Command("DELETE", 0x40, 0x00, false));

        // Act
        await _service.DeleteAsync(created.Id);

        // Assert
        Command? result = await _service.GetByIdAsync(created.Id);
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
        Command command = await _service.AddAsync(new Command("STATE_TEST", 0x50, 0x00, false));

        // Act
        await _service.SetDeviceStateAsync(command.Id, 10, true);

        // Assert
        CommandDeviceState? state = await _service.GetDeviceStateAsync(command.Id, 10);
        Assert.NotNull(state);
        Assert.True(state.IsEnabled);
    }

    [Fact]
    public async Task SetDeviceStateAsync_ExistingState_UpdatesState()
    {
        // Arrange
        Command command = await _service.AddAsync(new Command("UPDATE_STATE", 0x51, 0x00, false));
        await _service.SetDeviceStateAsync(command.Id, 3, true);

        // Act
        await _service.SetDeviceStateAsync(command.Id, 3, false);

        // Assert
        CommandDeviceState? state = await _service.GetDeviceStateAsync(command.Id, 3);
        Assert.NotNull(state);
        Assert.False(state.IsEnabled);
    }

    [Fact]
    public async Task SetDeviceStateAsync_NonExistingCommand_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SetDeviceStateAsync(999, 7, true));
    }

    [Fact]
    public async Task GetDeviceStateAsync_NonExistingState_ReturnsNull()
    {
        // Arrange
        Command command = await _service.AddAsync(new Command("NO_STATE", 0x52, 0x00, false));

        // Act
        CommandDeviceState? result = await _service.GetDeviceStateAsync(command.Id, 11);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWithDeviceStatesAsync_LoadsAllStates()
    {
        // Arrange
        Command command = await _service.AddAsync(new Command("MULTI_STATE", 0x53, 0x00, false));
        await _service.SetDeviceStateAsync(command.Id, 10, true);
        await _service.SetDeviceStateAsync(command.Id, 3, false);
        await _service.SetDeviceStateAsync(command.Id, 7, true);

        // Act
        Command? result = await _service.GetWithDeviceStatesAsync(command.Id);

        // Assert
        Assert.NotNull(result);
        // Stati sono caricati ma accessibili tramite GetDeviceStateAsync
        CommandDeviceState? state1 = await _service.GetDeviceStateAsync(command.Id, 10);
        CommandDeviceState? state2 = await _service.GetDeviceStateAsync(command.Id, 3);
        Assert.True(state1!.IsEnabled);
        Assert.False(state2!.IsEnabled);
    }

    [Fact]
    public async Task FullCode_AfterRetrieval_IsCorrect()
    {
        // Arrange
        Command command = await _service.AddAsync(new Command("FULLCODE", 0x12, 0x34, false));

        // Act
        Command? result = await _service.GetByIdAsync(command.Id);

        // Assert
        Assert.Equal(0x1234, result!.FullCode);
    }

    // === GetDeviceStatesForDeviceAsync ===

    [Fact]
    public async Task GetDeviceStatesForDeviceAsync_ReturnsStatesForDevice()
    {
        // Arrange — 2 comandi, entrambi con stato per Spark
        Command cmd1 = await _service.AddAsync(new Command("CMD_A", 0x60, 0x00, false));
        Command cmd2 = await _service.AddAsync(new Command("CMD_B", 0x61, 0x00, false));
        await _service.SetDeviceStateAsync(cmd1.Id, 7, false);
        await _service.SetDeviceStateAsync(cmd2.Id, 7, true);

        // Act
        IReadOnlyList<CommandDeviceState> result = await _service.GetDeviceStatesForDeviceAsync(7);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(7, s.DeviceId));
    }

    [Fact]
    public async Task GetDeviceStatesForDeviceAsync_ExcludesOtherDevices()
    {
        // Arrange — stato per Spark e EdenXp
        Command cmd = await _service.AddAsync(new Command("CMD_MULTI", 0x62, 0x00, false));
        await _service.SetDeviceStateAsync(cmd.Id, 7, false);
        await _service.SetDeviceStateAsync(cmd.Id, 3, true);

        // Act — solo Spark
        IReadOnlyList<CommandDeviceState> result = await _service.GetDeviceStatesForDeviceAsync(7);

        // Assert
        Assert.Single(result);
        Assert.Equal(cmd.Id, result[0].CommandId);
        Assert.False(result[0].IsEnabled);
    }

    [Fact]
    public async Task GetDeviceStatesForDeviceAsync_NoStates_ReturnsEmptyList()
    {
        // Arrange — comando senza stati
        await _service.AddAsync(new Command("CMD_NOSTATES", 0x63, 0x00, false));

        // Act
        IReadOnlyList<CommandDeviceState> result = await _service.GetDeviceStatesForDeviceAsync(11);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetDeviceStatesForDeviceAsync_MapsToDomainCorrectly()
    {
        // Arrange
        Command cmd = await _service.AddAsync(new Command("CMD_MAP", 0x64, 0x00, false));
        await _service.SetDeviceStateAsync(cmd.Id, 4, false);

        // Act
        IReadOnlyList<CommandDeviceState> result = await _service.GetDeviceStatesForDeviceAsync(4);

        // Assert — verifica mapping domain
        CommandDeviceState state = Assert.Single(result);
        Assert.Equal(cmd.Id, state.CommandId);
        Assert.Equal(4, state.DeviceId);
        Assert.False(state.IsEnabled);
    }
}
