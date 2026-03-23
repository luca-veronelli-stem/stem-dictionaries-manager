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
        // Assert
        Assert.Null(_viewModel.DeviceType);
        Assert.Empty(_viewModel.DeviceName);
        Assert.Empty(_viewModel.Dictionaries);
        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.ErrorMessage);
    }

    [Fact]
    public void OnCurrentViewChanged_SetsDeviceTypeAndName()
    {
        // Act - simula navigazione a DeviceDetail
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = DeviceType.OptimusXp
        });

        // Assert
        Assert.Equal(DeviceType.OptimusXp, _viewModel.DeviceType);
        Assert.Equal("Optimus XP", _viewModel.DeviceName);
    }

    [Fact]
    public void OnCurrentViewChanged_SherpaSlim_SetsCorrectName()
    {
        // Act
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = DeviceType.SherpaSlim
        });

        // Assert
        Assert.Equal("Sherpa Slim", _viewModel.DeviceName);
    }

    [Fact]
    public async Task OnCurrentViewChanged_LoadsDictionaries()
    {
        // Arrange - prepara dati: un boardType, un dizionario, una board del device
        var boardType = new BoardType("Madre OptimusXP", 17);
        _boardService.SeedBoardTypes(boardType);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        var board = new Board(DeviceType.OptimusXp, bt, "Madre #1", 1);
        await _boardService.AddAsync(board);

        var dict = new Dictionary("Optimus XP", DeviceType.OptimusXp, bt, "Variabili Optimus XP");
        _dictionaryService.SeedData(dict);

        // Act - simula navigazione
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = DeviceType.OptimusXp
        });

        // Aspetta che LoadDictionariesAsync finisca (fire-and-forget)
        await Task.Delay(200);

        // Assert
        Assert.NotEmpty(_viewModel.Dictionaries);
    }

    [Fact]
    public void OnCurrentViewChanged_OtherView_DoesNothing()
    {
        // Act - naviga a una view diversa da DeviceDetail
        _navigationService.NavigateTo(ViewType.DictionaryList);

        // Assert - non cambia nulla
        Assert.Null(_viewModel.DeviceType);
        Assert.Empty(_viewModel.DeviceName);
    }

    [Fact]
    public void OnCurrentViewChanged_NullDeviceType_DoesNothing()
    {
        // Act - naviga a DeviceDetail senza DeviceType
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter());

        // Assert
        Assert.Null(_viewModel.DeviceType);
    }

    [Fact]
    public void GoBackCommand_CallsNavigationGoBack()
    {
        // Arrange
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = DeviceType.OptimusXp
        });

        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public void OpenDictionaryCommand_WithNull_DoesNotNavigate()
    {
        // Arrange
        _viewModel.SelectedDictionary = null;
        var historyBefore = _navigationService.NavigationHistory.Count;

        // Act
        _viewModel.OpenDictionaryCommand.Execute(null);

        // Assert - nessuna navigazione aggiuntiva
        Assert.Equal(historyBefore, _navigationService.NavigationHistory.Count);
    }

    [Fact]
    public void OpenDictionaryCommand_WithSelection_NavigatesToVariableList()
    {
        // Arrange
        _viewModel.SelectedDictionary = new DictionaryItem(42, "Test Dict", "Madre", 10);

        // Act
        _viewModel.OpenDictionaryCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.VariableList, _navigationService.LastNavigatedView);
        Assert.Equal(42, _navigationService.LastParameter?.ParentId);
    }

    [Fact]
    public void ClearSubscriptions_PreventsEventHandling()
    {
        // Arrange
        _viewModel.ClearSubscriptions();

        // Act - naviga a DeviceDetail, non deve essere gestito
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = DeviceType.Gradino
        });

        // Assert - non deve aver reagito all'evento
        Assert.Null(_viewModel.DeviceType);
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
    public void DeviceName_MapsAllDeviceTypes(DeviceType deviceType, string expectedName)
    {
        // Act
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = deviceType
        });

        // Assert
        Assert.Equal(expectedName, _viewModel.DeviceName);
    }
}
#endif
