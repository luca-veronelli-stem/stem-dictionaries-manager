#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DeviceDetailViewModel.
/// </summary>
public class DeviceDetailViewModelTests
{
    private readonly MockNavigationService _navigationService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockBoardService _boardService;
    private readonly DeviceDetailViewModel _viewModel;

    public DeviceDetailViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _dictionaryService = new MockDictionaryService();
        _boardService = new MockBoardService();

        _viewModel = new DeviceDetailViewModel(
            _navigationService,
            _dictionaryService,
            _boardService);
    }

    [Fact]
    public void Constructor_DefaultValues()
    {
        Assert.Null(_viewModel.DeviceType);
        Assert.Empty(_viewModel.DeviceName);
        Assert.Empty(_viewModel.Dictionaries);
        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_SetsDeviceTypeAndName()
    {
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        Assert.Equal(DeviceType.OptimusXp, _viewModel.DeviceType);
        Assert.Equal("Optimus XP", _viewModel.DeviceName);
    }

    [Fact]
    public async Task LoadAsync_SherpaSlim_SetsCorrectName()
    {
        await _viewModel.LoadAsync(DeviceType.SherpaSlim);

        Assert.Equal("Sherpa Slim", _viewModel.DeviceName);
    }

    [Fact]
    public async Task LoadAsync_LoadsDictionaries()
    {
        // Arrange
        var boardType = new BoardType("Madre OptimusXP", 17);
        _boardService.SeedBoardTypes(boardType);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        var board = new Board(DeviceType.OptimusXp, bt, "Madre #1", 1);
        await _boardService.AddAsync(board);

        var dict = new Dictionary("Optimus XP", DeviceType.OptimusXp, bt, "Variabili Optimus XP");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert
        Assert.NotEmpty(_viewModel.Dictionaries);
    }

    [Fact]
    public void GoBackCommand_CallsNavigationGoBack()
    {
        _viewModel.GoBackCommand.Execute(null);

        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public void OpenDictionaryCommand_WithNull_DoesNotNavigate()
    {
        _viewModel.SelectedDictionary = null;
        var historyBefore = _navigationService.NavigationHistory.Count;

        _viewModel.OpenDictionaryCommand.Execute(null);

        Assert.Equal(historyBefore, _navigationService.NavigationHistory.Count);
    }

    [Fact]
    public void OpenDictionaryCommand_WithSelection_NavigatesToVariableList()
    {
        _viewModel.SelectedDictionary = new DictionaryItem(42, "Test Dict", "Madre", 10);

        _viewModel.OpenDictionaryCommand.Execute(null);

        Assert.Equal(ViewType.VariableList, _navigationService.LastNavigatedView);
        Assert.Equal(42, _navigationService.LastParameter?.ParentId);
    }

    [Theory]
    [InlineData(DeviceType.SherpaSlim, "Sherpa Slim")]
    [InlineData(DeviceType.TopLiftM, "TopLift M")]
    [InlineData(DeviceType.EdenXp, "Eden XP")]
    [InlineData(DeviceType.Gradino, "Gradino")]
    [InlineData(DeviceType.Spyke, "Spyke")]
    [InlineData(DeviceType.Spark, "Spark")]
    [InlineData(DeviceType.TopLiftA2, "TopLift A2")]
    [InlineData(DeviceType.O3zTech, "O3z Tech")]
    [InlineData(DeviceType.OptimusXp, "Optimus XP")]
    [InlineData(DeviceType.R3lXp, "R3L-XP")]
    [InlineData(DeviceType.EdenBs8, "Eden BS8")]
    public async Task DeviceName_MapsAllDeviceTypes(DeviceType deviceType, string expectedName)
    {
        await _viewModel.LoadAsync(deviceType);

        Assert.Equal(expectedName, _viewModel.DeviceName);
    }
}
#endif
