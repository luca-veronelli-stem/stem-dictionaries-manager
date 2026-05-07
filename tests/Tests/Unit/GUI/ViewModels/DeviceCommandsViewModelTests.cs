#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

public class DeviceCommandsViewModelTests
{
    private readonly MockCommandService _commandService = new();
    private readonly MockDeviceService _deviceService = new();
    private readonly MockNavigationService _navigationService = new();
    private readonly MockMessageService _messageService = new();
    private readonly DeviceCommandsViewModel _viewModel;

    public DeviceCommandsViewModelTests()
    {
        _deviceService.SeedDefaultDevices();
        _viewModel = new DeviceCommandsViewModel(
            _commandService, _deviceService, _navigationService, _messageService);
    }

    // === Defaults ===

    [Fact]
    public void Constructor_DefaultValues()
    {
        Assert.Null(_viewModel.DeviceId);
        Assert.Equal(string.Empty, _viewModel.DeviceName);
        Assert.Empty(_viewModel.Commands);
        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.ErrorMessage);
        Assert.False(_viewModel.HasChanges);
    }

    // === LoadAsync ===

    [Fact]
    public async Task LoadAsync_SetsDeviceTypeAndName()
    {
        await _viewModel.LoadAsync(3);

        Assert.Equal(3, _viewModel.DeviceId);
        Assert.Equal("Eden-XP", _viewModel.DeviceName);
    }

    [Fact]
    public async Task LoadAsync_PopulatesCommands()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false),
            new Command("Write Variable", 0x00, 0x02, false));

        await _viewModel.LoadAsync(7);

        Assert.Equal(2, _viewModel.Commands.Count);
    }

    [Fact]
    public async Task LoadAsync_DefaultIsEnabled_True()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));

        await _viewModel.LoadAsync(7);

        Assert.True(_viewModel.Commands[0].IsEnabled);
        Assert.True(_viewModel.Commands[0].OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_WithOverride_UsesOverrideState()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));
        var cmd = (await _commandService.GetAllAsync())[0];
        _commandService.SeedDeviceStates(
            new CommandDeviceState(cmd.Id, 7, false));

        await _viewModel.LoadAsync(7);

        Assert.False(_viewModel.Commands[0].IsEnabled);
        Assert.False(_viewModel.Commands[0].OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_OverrideForOtherDevice_Ignored()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));
        var cmd = (await _commandService.GetAllAsync())[0];
        _commandService.SeedDeviceStates(
            new CommandDeviceState(cmd.Id, 3, false));

        await _viewModel.LoadAsync(7);

        // Spark non ha override, default = true
        Assert.True(_viewModel.Commands[0].IsEnabled);
    }

    [Fact]
    public async Task LoadAsync_CommandItem_MapsProperties()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));

        await _viewModel.LoadAsync(7);

        var item = _viewModel.Commands[0];
        Assert.Equal("Read Variable", item.Name);
        Assert.Equal("0x0001", item.FullCode);
        Assert.Equal("Comando", item.TypeDisplay);
    }

    [Fact]
    public async Task LoadAsync_ResponseCommand_ShowsResponse()
    {
        _commandService.SeedData(
            new Command("Read Variable Response", 0x80, 0x01, true));

        await _viewModel.LoadAsync(7);

        Assert.Equal("Response", _viewModel.Commands[0].TypeDisplay);
        Assert.Equal("0x8001", _viewModel.Commands[0].FullCode);
    }

    [Fact]
    public async Task LoadAsync_ServiceThrows_SetsErrorMessage()
    {
        _commandService.ExceptionToThrow = new InvalidOperationException("DB error");

        await _viewModel.LoadAsync(7);

        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("DB error", _viewModel.ErrorMessage);
        Assert.Empty(_viewModel.Commands);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingFalseAfterCompletion()
    {
        await _viewModel.LoadAsync(7);
        Assert.False(_viewModel.IsLoading);
    }

    // === HasChanges ===

    [Fact]
    public async Task HasChanges_NoToggle_False()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));

        await _viewModel.LoadAsync(7);

        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public async Task HasChanges_AfterToggle_True()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));

        await _viewModel.LoadAsync(7);
        _viewModel.Commands[0].IsEnabled = false;

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task HasChanges_ToggleBackToOriginal_False()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));

        await _viewModel.LoadAsync(7);
        _viewModel.Commands[0].IsEnabled = false;
        _viewModel.Commands[0].IsEnabled = true;

        Assert.False(_viewModel.HasChanges);
    }

    // === SaveCommand ===

    [Fact]
    public async Task Save_NoChanges_ShowsInfoMessage()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));
        await _viewModel.LoadAsync(7);

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("No changes", _messageService.CurrentMessage ?? "");
    }

    [Fact]
    public async Task Save_WithChanges_CallsSetDeviceState()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false),
            new Command("Write Variable", 0x00, 0x02, false));
        await _viewModel.LoadAsync(7);
        _commandService.MethodCalls.Clear();

        // Disabilita solo il primo
        _viewModel.Commands[0].IsEnabled = false;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Solo il comando modificato deve generare SetDeviceStateAsync
        Assert.Contains(_commandService.MethodCalls,
            c => c.StartsWith("SetDeviceStateAsync:") && c.Contains("False"));
    }

    [Fact]
    public async Task Save_WithChanges_ShowsSuccessMessage()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));
        await _viewModel.LoadAsync(7);
        _viewModel.Commands[0].IsEnabled = false;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Salvati", _messageService.CurrentMessage ?? "");
    }

    [Fact]
    public async Task Save_ServiceThrows_ShowsErrorMessage()
    {
        _commandService.SeedData(
            new Command("Read Variable", 0x00, 0x01, false));
        await _viewModel.LoadAsync(7);
        _viewModel.Commands[0].IsEnabled = false;
        _commandService.ExceptionToThrow = new InvalidOperationException("Save failed");

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Save failed", _messageService.CurrentMessage ?? "");
    }

    // === GoBack ===

    [Fact]
    public void GoBack_CallsNavigationGoBack()
    {
        _viewModel.GoBackCommand.Execute(null);

        Assert.True(_navigationService.GoBackCalled);
    }

    // === DeviceDetail navigazione entry Comandi ===

    [Fact]
    public async Task DeviceDetail_OpenComandiEntry_NavigatesToDeviceCommands()
    {
        var dictionaryService = new MockDictionaryService();
        var boardService = new MockBoardService();
        var deviceService = new MockDeviceService();
        var commandService = new MockCommandService();
        var dialogService = new MockDialogService();
        var messageService = new MockMessageService();
        var navigationService = new MockNavigationService();
        deviceService.SeedDefaultDevices();

        var vm = new DeviceDetailViewModel(
            navigationService, dictionaryService, boardService, deviceService,
            commandService, dialogService, messageService);

        await vm.LoadAsync(7);

        // The last item must be "Commands"
        var commandsEntry = vm.Dictionaries.Last();
        Assert.True(commandsEntry.IsCommandsEntry);
        Assert.Equal("Commands", commandsEntry.Name);

        // Select and open
        vm.SelectedDictionary = commandsEntry;
        vm.OpenDictionaryCommand.Execute(null);

        // Should navigate to DeviceCommands with deviceId
        Assert.Equal(ViewType.DeviceCommands, navigationService.LastNavigatedView);
        Assert.Equal(7, navigationService.LastParameter?.DeviceId);
    }
}
#endif
