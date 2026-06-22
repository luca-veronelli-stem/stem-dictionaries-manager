#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DeviceListViewModel.
/// SESSION_035: carica da DB tramite IDeviceService.
/// </summary>
public class DeviceListViewModelTests
{
    private readonly MockNavigationService _navigationService;
    private readonly MockDeviceService _deviceService;
    private readonly MockBoardService _boardService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DeviceListViewModel _viewModel;

    public DeviceListViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _deviceService = new MockDeviceService();
        _boardService = new MockBoardService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();
        _deviceService.SeedDefaultDevices();
        _viewModel = new DeviceListViewModel(
            _navigationService, _deviceService, _boardService, _dialogService, _messageService,
            NullLogger<DeviceListViewModel>.Instance);
    }

    [Fact]
    public async Task LoadAsync_LoadsAllDevices()
    {
        await _viewModel.LoadAsync();

        Assert.Equal(11, _viewModel.Devices.Count);
    }

    [Fact]
    public async Task SearchText_FiltersListByName()
    {
        await _viewModel.LoadAsync();

        _viewModel.SearchText = "Sherpa";

        Assert.Single(_viewModel.Devices);
        Assert.Equal("Sherpa Slim", _viewModel.Devices[0].Name);
    }

    [Fact]
    public async Task SearchText_FiltersListByDescription()
    {
        await _viewModel.LoadAsync();

        _viewModel.SearchText = "sanificazione";

        Assert.Single(_viewModel.Devices);
        Assert.Equal("O3Z-Tech", _viewModel.Devices[0].Name);
    }

    [Fact]
    public async Task SearchText_CaseInsensitive()
    {
        await _viewModel.LoadAsync();

        _viewModel.SearchText = "SPARK";

        Assert.Single(_viewModel.Devices);
        Assert.Equal("Spark", _viewModel.Devices[0].Name);
    }

    [Fact]
    public async Task SearchText_EmptyString_ShowsAll()
    {
        await _viewModel.LoadAsync();
        _viewModel.SearchText = "Sherpa";
        Assert.Single(_viewModel.Devices);

        _viewModel.SearchText = string.Empty;

        Assert.Equal(11, _viewModel.Devices.Count);
    }

    [Fact]
    public async Task SearchText_NoMatch_ShowsEmpty()
    {
        await _viewModel.LoadAsync();

        _viewModel.SearchText = "zzzzzzz";

        Assert.Empty(_viewModel.Devices);
    }

    [Fact]
    public async Task SelectDeviceCommand_SetsSelectedDevice()
    {
        await _viewModel.LoadAsync();
        var device = _viewModel.Devices[0];

        _viewModel.SelectDeviceCommand.Execute(device);

        Assert.Equal(device, _viewModel.SelectedDevice);
    }

    [Fact]
    public async Task OpenDeviceCommand_NavigatesToDeviceDetail()
    {
        await _viewModel.LoadAsync();
        var device = _viewModel.Devices.First(d => d.Name == "Optimus-XP");

        _viewModel.OpenDeviceCommand.Execute(device);

        Assert.Equal(ViewType.DeviceDetail, _navigationService.LastNavigatedView);
        Assert.Equal(device.DeviceId, _navigationService.LastParameter?.DeviceId);
    }

    [Fact]
    public async Task OpenDeviceCommand_WithNull_UsesSelectedDevice()
    {
        await _viewModel.LoadAsync();
        var device = _viewModel.Devices.First(d => d.Name == "Eden-XP");
        _viewModel.SelectedDevice = device;

        _viewModel.OpenDeviceCommand.Execute(null);

        Assert.Equal(ViewType.DeviceDetail, _navigationService.LastNavigatedView);
        Assert.Equal(device.DeviceId, _navigationService.LastParameter?.DeviceId);
    }

    [Fact]
    public void OpenDeviceCommand_WithNullAndNoSelection_DoesNotNavigate()
    {
        _viewModel.SelectedDevice = null;

        _viewModel.OpenDeviceCommand.Execute(null);

        Assert.Empty(_navigationService.NavigationHistory);
    }

    [Fact]
    public async Task DeviceItem_HasCorrectProperties()
    {
        await _viewModel.LoadAsync();

        var sherpa = _viewModel.Devices.First(d => d.Name == "Sherpa Slim");
        Assert.False(string.IsNullOrWhiteSpace(sherpa.Description));
        Assert.Equal(1, sherpa.MachineCode);
    }

    [Fact]
    public void AddDeviceCommand_NavigatesToDeviceEdit()
    {
        _viewModel.AddDeviceCommand.Execute(null);

        Assert.Equal(ViewType.DeviceEdit, _navigationService.LastNavigatedView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public async Task LoadAsync_BoardAndDictionaryCounts_Correct()
    {
        // Arrange: device con 3 board, 2 dizionari distinti
        var deviceService = new MockDeviceService();
        var boardService = new MockBoardService();
        deviceService.SeedData(Device.Restore(1, "TestDevice", 1, null));
        boardService.SeedBoards(
            Board.Restore(1, 1, "Madre", 5, 1, null, true, dictionaryId: 10, machineCode: 1),
            Board.Restore(2, 1, "Puls 1", 4, 2, null, false, dictionaryId: 20, machineCode: 1),
            Board.Restore(3, 1, "Puls 2", 4, 3, null, false, dictionaryId: 20, machineCode: 1));

        var vm = new DeviceListViewModel(
            _navigationService, deviceService, boardService, _dialogService, _messageService,
            NullLogger<DeviceListViewModel>.Instance);

        // Act
        await vm.LoadAsync();

        // Assert
        var item = vm.Devices.Single();
        Assert.Equal(3, item.BoardCount);
        Assert.Equal(2, item.DictionaryCount); // dict 10 e 20
    }
}
#endif
