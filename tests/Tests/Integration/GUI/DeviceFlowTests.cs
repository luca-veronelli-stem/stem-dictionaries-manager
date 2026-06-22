#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso CRUD dispositivi.
/// Testa F5 (esplorazione dispositivi) e CRUD Device.
/// </summary>
public class DeviceFlowTests
{
    private readonly MockDeviceService _deviceService;
    private readonly MockBoardService _boardService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;

    public DeviceFlowTests()
    {
        _deviceService = new MockDeviceService();
        _boardService = new MockBoardService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();
    }

    #region DeviceListViewModel Tests

    [Fact]
    public async Task DeviceList_LoadsAllDevices()
    {
        // Arrange
        _deviceService.SeedData(
            Device.Restore(1, "Eden-XP", 3, "Test 1"),
            Device.Restore(2, "Spark", 7, "Test 2")
        );
        var viewModel = new DeviceListViewModel(
            _navigationService, _deviceService, _boardService, _dialogService, _messageService,
            NullLogger<DeviceListViewModel>.Instance);

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.Equal(2, viewModel.Devices.Count);
    }

    [Fact]
    public async Task DeviceList_Search_FiltersCorrectly()
    {
        // Arrange
        _deviceService.SeedData(
            Device.Restore(1, "Eden-XP", 3, "Supporto barella"),
            Device.Restore(2, "Spark", 7, "Barella robotizzata"),
            Device.Restore(3, "Optimus-XP", 10, "Supporto elettrico")
        );
        var viewModel = new DeviceListViewModel(
            _navigationService, _deviceService, _boardService, _dialogService, _messageService,
            NullLogger<DeviceListViewModel>.Instance);
        await viewModel.LoadAsync();

        // Act
        viewModel.SearchText = "Eden";

        // Assert
        Assert.Single(viewModel.Devices);
        Assert.Equal("Eden-XP", viewModel.Devices[0].Name);
    }

    [Fact]
    public async Task DeviceList_OpenDevice_NavigatesToDetail()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        var viewModel = new DeviceListViewModel(
            _navigationService, _deviceService, _boardService, _dialogService, _messageService,
            NullLogger<DeviceListViewModel>.Instance);
        await viewModel.LoadAsync();

        // Act
        viewModel.SelectedDevice = viewModel.Devices[0];
        viewModel.OpenDeviceCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DeviceDetail, _navigationService.LastNavigatedView);
        Assert.Equal(1, _navigationService.LastParameter?.DeviceId);
    }

    [Fact]
    public async Task DeviceList_NewDevice_NavigatesToEdit()
    {
        // Arrange
        var viewModel = new DeviceListViewModel(
            _navigationService, _deviceService, _boardService, _dialogService, _messageService,
            NullLogger<DeviceListViewModel>.Instance);

        // Act
        viewModel.AddDeviceCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DeviceEdit, _navigationService.LastNavigatedView);
    }

    #endregion

    #region DeviceEditViewModel Tests

    [Fact]
    public async Task CreateDevice_WithValidData_SavesAndNavigatesBack()
    {
        // Arrange
        var viewModel = new DeviceEditViewModel(
            _deviceService, _boardService, _navigationService, _dialogService, _messageService,
            NullLogger<DeviceEditViewModel>.Instance);
        await viewModel.InitializeAsync(null);

        viewModel.Name = "NewDevice";
        viewModel.MachineCode = "99";
        viewModel.Description = "Test device";

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_deviceService.MethodCalls, m => m.StartsWith("AddAsync:NewDevice"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CreateDevice_DuplicateName_ShowsError()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Existing", 3, null));
        var viewModel = new DeviceEditViewModel(
            _deviceService, _boardService, _navigationService, _dialogService, _messageService,
            NullLogger<DeviceEditViewModel>.Instance);
        await viewModel.InitializeAsync(null);

        viewModel.Name = "NewDevice";
        viewModel.MachineCode = "99";

        // Imposta eccezione DOPO InitializeAsync
        _deviceService.ExceptionToThrow = new InvalidOperationException(
            "Esiste già un dispositivo con nome 'NewDevice'.");

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - verifica sia dialog che message (dipende da implementazione)
        Assert.True(_dialogService.ShowErrorCalled || _messageService.Messages.Any(m => m.Severity == MessageSeverity.Error));
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CreateDevice_MachineCode6_ReservedBLE_ShowsError()
    {
        // Arrange
        var viewModel = new DeviceEditViewModel(
            _deviceService, _boardService, _navigationService, _dialogService, _messageService,
            NullLogger<DeviceEditViewModel>.Instance);
        await viewModel.InitializeAsync(null);

        viewModel.Name = "TestBLE";
        viewModel.MachineCode = "6"; // BR-015: riservato per BLE

        // Imposta eccezione DOPO InitializeAsync
        _deviceService.ExceptionToThrow = new InvalidOperationException(
            "MachineCode 6 è riservato per BLE Module.");

        // Act
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - verifica sia dialog che message
        Assert.True(_dialogService.ShowErrorCalled || _messageService.Messages.Any(m => m.Severity == MessageSeverity.Error));
    }

    [Fact]
    public async Task EditDevice_LoadsExistingData()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test description"));
        var viewModel = new DeviceEditViewModel(
            _deviceService, _boardService, _navigationService, _dialogService, _messageService,
            NullLogger<DeviceEditViewModel>.Instance);

        // Act
        await viewModel.InitializeAsync(deviceId: 1);

        // Assert
        Assert.Equal("Eden-XP", viewModel.Name);
        Assert.Equal("3", viewModel.MachineCode);
        Assert.Equal("Test description", viewModel.Description);
        Assert.False(viewModel.IsNew);
    }

    [Fact]
    public async Task DeleteDevice_WithConfirmation_Deletes()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "ToDelete", 99, null));
        _dialogService.ConfirmResult = DialogResult.Yes;
        var viewModel = new DeviceEditViewModel(
            _deviceService, _boardService, _navigationService, _dialogService, _messageService,
            NullLogger<DeviceEditViewModel>.Instance);
        await viewModel.InitializeAsync(deviceId: 1);

        // Act
        await viewModel.DeleteDeviceCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_deviceService.MethodCalls, m => m == "DeleteAsync:1");
        Assert.True(_navigationService.GoBackCalled);
    }

    #endregion
}
#endif
