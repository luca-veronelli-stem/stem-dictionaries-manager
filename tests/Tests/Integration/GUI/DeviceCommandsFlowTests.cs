#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso DeviceCommands (F5.3).
/// Testa stato comandi per device con checkbox Attivo e salvataggio bulk.
/// </summary>
public class DeviceCommandsFlowTests
{
    private readonly MockCommandService _commandService;
    private readonly MockDeviceService _deviceService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DeviceCommandsViewModel _viewModel;

    public DeviceCommandsFlowTests()
    {
        _commandService = new MockCommandService();
        _deviceService = new MockDeviceService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        // Seed dati base
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test device"));
        _commandService.SeedData(
            Command.Restore(1, "ReadVariable", 0x00, 0x01, false, []),
            Command.Restore(2, "WriteVariable", 0x00, 0x02, false, []),
            Command.Restore(3, "ReadVariableResponse", 0x80, 0x01, true, [])
        );

        _viewModel = new DeviceCommandsViewModel(
            _commandService,
            _deviceService,
            _navigationService,
            _messageService);
    }

    #region Load Tests

    [Fact]
    public async Task LoadCommands_ShowsAllCommandsWithDefaultEnabled()
    {
        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert
        Assert.Equal(3, _viewModel.Commands.Count);
        Assert.All(_viewModel.Commands, cmd => Assert.True(cmd.IsEnabled));
    }

    [Fact]
    public async Task LoadCommands_WithExistingOverrides_ShowsOverrideState()
    {
        // Arrange - override: ReadVariable disabilitato per device 1
        _commandService.SeedDeviceStates(
            CommandDeviceState.Restore(1, 1, 1, isEnabled: false)
        );

        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert
        var readVar = _viewModel.Commands.First(c => c.Name == "ReadVariable");
        Assert.False(readVar.IsEnabled);

        var writeVar = _viewModel.Commands.First(c => c.Name == "WriteVariable");
        Assert.True(writeVar.IsEnabled); // Default
    }

    [Fact]
    public async Task LoadCommands_OverrideForOtherDevice_Ignored()
    {
        // Arrange - override per device 99, non per device 1
        _commandService.SeedDeviceStates(
            CommandDeviceState.Restore(1, 1, 99, isEnabled: false)
        );

        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert - tutti abilitati (override ignorato)
        Assert.All(_viewModel.Commands, cmd => Assert.True(cmd.IsEnabled));
    }

    #endregion

    #region Toggle Tests

    [Fact]
    public async Task ToggleCommand_SetsHasChanges()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        Assert.False(_viewModel.HasChanges);

        // Act
        _viewModel.Commands[0].IsEnabled = false;

        // Assert
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task ToggleCommand_BackToOriginal_ClearsHasChanges()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        var original = _viewModel.Commands[0].IsEnabled;
        _viewModel.Commands[0].IsEnabled = !original; // Toggle
        Assert.True(_viewModel.HasChanges);

        // Act
        _viewModel.Commands[0].IsEnabled = original; // Back to original

        // Assert
        Assert.False(_viewModel.HasChanges);
    }

    #endregion

    #region Save Tests

    [Fact]
    public async Task SaveChanges_OnlyUpdatesModifiedCommands()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        _viewModel.Commands[0].IsEnabled = false; // Modifica solo il primo

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - solo un SetDeviceStateAsync chiamato
        var setCalls = _commandService.MethodCalls.Where(m => m.StartsWith("SetDeviceStateAsync")).ToList();
        Assert.Single(setCalls);
        Assert.Contains("SetDeviceStateAsync:1:1:False", setCalls);
    }

    [Fact]
    public async Task SaveChanges_NoChanges_ShowsInfoMessage()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        // No changes

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Info);
    }

    [Fact]
    public async Task SaveChanges_WithChanges_ShowsSuccessMessage()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        _viewModel.Commands[0].IsEnabled = false;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveChanges_OnError_ShowsErrorMessage()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        _viewModel.Commands[0].IsEnabled = false;
        _commandService.ExceptionToThrow = new InvalidOperationException("Test error");

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Error);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public async Task GoBack_NavigatesBack()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);

        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    #endregion
}
#endif
