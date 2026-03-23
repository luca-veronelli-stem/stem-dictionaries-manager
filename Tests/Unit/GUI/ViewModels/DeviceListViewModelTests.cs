#if WINDOWS
using Core.Enums;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DeviceListViewModel.
/// </summary>
public class DeviceListViewModelTests
{
    private readonly MockNavigationService _navigationService;
    private readonly DeviceListViewModel _viewModel;

    public DeviceListViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _viewModel = new DeviceListViewModel(_navigationService);
    }

    [Fact]
    public void Constructor_LoadsAllDevices()
    {
        // Assert - tutti gli 11 DeviceType caricati
        var deviceTypeCount = Enum.GetValues<DeviceType>().Length;
        Assert.Equal(deviceTypeCount, _viewModel.Devices.Count);
    }

    [Fact]
    public void Devices_ContainsAllDeviceTypes()
    {
        // Assert
        foreach (var deviceType in Enum.GetValues<DeviceType>())
        {
            Assert.Contains(_viewModel.Devices, d => d.DeviceType == deviceType);
        }
    }

    [Fact]
    public void SearchText_FiltersListByName()
    {
        // Act
        _viewModel.SearchText = "Sherpa";

        // Assert
        Assert.Single(_viewModel.Devices);
        Assert.Equal(DeviceType.SherpaSlim, _viewModel.Devices[0].DeviceType);
    }

    [Fact]
    public void SearchText_FiltersListByDescription()
    {
        // Act
        _viewModel.SearchText = "sanificazione";

        // Assert
        Assert.Single(_viewModel.Devices);
        Assert.Equal(DeviceType.O3zTech, _viewModel.Devices[0].DeviceType);
    }

    [Fact]
    public void SearchText_CaseInsensitive()
    {
        // Act
        _viewModel.SearchText = "SPARK";

        // Assert
        Assert.Single(_viewModel.Devices);
        Assert.Equal(DeviceType.Spark, _viewModel.Devices[0].DeviceType);
    }

    [Fact]
    public void SearchText_EmptyString_ShowsAll()
    {
        // Arrange
        _viewModel.SearchText = "Sherpa";
        Assert.Single(_viewModel.Devices);

        // Act
        _viewModel.SearchText = string.Empty;

        // Assert
        Assert.Equal(Enum.GetValues<DeviceType>().Length, _viewModel.Devices.Count);
    }

    [Fact]
    public void SearchText_NoMatch_ShowsEmpty()
    {
        // Act
        _viewModel.SearchText = "zzzzzzz";

        // Assert
        Assert.Empty(_viewModel.Devices);
    }

    [Fact]
    public void SelectDeviceCommand_SetsSelectedDevice()
    {
        // Arrange
        var device = _viewModel.Devices[0];

        // Act
        _viewModel.SelectDeviceCommand.Execute(device);

        // Assert
        Assert.Equal(device, _viewModel.SelectedDevice);
    }

    [Fact]
    public void OpenDeviceCommand_NavigatesToDeviceDetail()
    {
        // Arrange
        var device = _viewModel.Devices.First(d => d.DeviceType == DeviceType.OptimusXp);

        // Act
        _viewModel.OpenDeviceCommand.Execute(device);

        // Assert
        Assert.Equal(ViewType.DeviceDetail, _navigationService.LastNavigatedView);
        Assert.Equal(DeviceType.OptimusXp, _navigationService.LastParameter?.DeviceType);
    }

    [Fact]
    public void OpenDeviceCommand_WithNull_UsesSelectedDevice()
    {
        // Arrange
        var device = _viewModel.Devices.First(d => d.DeviceType == DeviceType.EdenXp);
        _viewModel.SelectedDevice = device;

        // Act
        _viewModel.OpenDeviceCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DeviceDetail, _navigationService.LastNavigatedView);
        Assert.Equal(DeviceType.EdenXp, _navigationService.LastParameter?.DeviceType);
    }

    [Fact]
    public void OpenDeviceCommand_WithNullAndNoSelection_DoesNotNavigate()
    {
        // Arrange
        _viewModel.SelectedDevice = null;

        // Act
        _viewModel.OpenDeviceCommand.Execute(null);

        // Assert
        Assert.Empty(_navigationService.NavigationHistory);
    }

    [Fact]
    public void DeviceItem_HasCorrectProperties()
    {
        // Assert
        var sherpa = _viewModel.Devices.First(d => d.DeviceType == DeviceType.SherpaSlim);
        Assert.Equal("Sherpa Slim", sherpa.Name);
        Assert.False(string.IsNullOrWhiteSpace(sherpa.Description));
    }
}
#endif
