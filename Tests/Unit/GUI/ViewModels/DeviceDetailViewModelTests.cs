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
        Assert.Equal("Optimus-XP", _viewModel.DeviceName);
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
    [InlineData(DeviceType.TopLiftM, "TopLift-M")]
    [InlineData(DeviceType.EdenXp, "Eden-XP")]
    [InlineData(DeviceType.Gradino, "Gradino")]
    [InlineData(DeviceType.Spyke, "Spyke")]
    [InlineData(DeviceType.Spark, "Spark")]
    [InlineData(DeviceType.TopLiftA2, "TopLift-A2")]
    [InlineData(DeviceType.O3zTech, "O3Z-Tech")]
    [InlineData(DeviceType.OptimusXp, "Optimus-XP")]
    [InlineData(DeviceType.R3lXp, "R3L-XP")]
    [InlineData(DeviceType.EdenBs8, "Eden-BS8")]
    public async Task DeviceName_MapsAllDeviceTypes(DeviceType deviceType, string expectedName)
    {
        await _viewModel.LoadAsync(deviceType);

        Assert.Equal(expectedName, _viewModel.DeviceName);
    }

    // === Test semantiche filtro dizionari ===

    [Fact]
    public async Task LoadAsync_StandardDictionary_VisibleForAnyDevice()
    {
        // Arrange — dizionario Standard (null, null)
        var dictStandard = new Dictionary("Standard", description: "Variabili comuni");
        _dictionaryService.SeedData(dictStandard);

        // Act — device senza board
        await _viewModel.LoadAsync(DeviceType.Spyke);

        // Assert — Standard è sempre visibile
        Assert.Single(_viewModel.Dictionaries);
        Assert.Equal("Standard", _viewModel.Dictionaries[0].Name);
    }

    [Fact]
    public async Task LoadAsync_SharedPeripheral_VisibleWhenDeviceHasMatchingBoard()
    {
        // Arrange — Pulsantiera condivisa (null, BT)
        var btPulsantiera = new BoardType("Pulsantiera 4x4", 4);
        _boardService.SeedBoardTypes(btPulsantiera);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        var board = new Board(DeviceType.OptimusXp, bt, "Tastiera 1", 1);
        await _boardService.AddAsync(board);

        var dictPulsantiere = new Dictionary("Pulsantiere 4x4", boardType: bt, description: "Condiviso");
        _dictionaryService.SeedData(dictPulsantiere);

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert — condiviso visibile perché il device ha una board con quel BoardType
        Assert.Single(_viewModel.Dictionaries);
        Assert.Equal("Pulsantiere 4x4", _viewModel.Dictionaries[0].Name);
    }

    [Fact]
    public async Task LoadAsync_SharedPeripheral_NotVisibleWhenDeviceLacksBoard()
    {
        // Arrange — Pulsantiera condivisa (null, BT)
        var btPulsantiera = new BoardType("Pulsantiera 4x4", 4);
        _boardService.SeedBoardTypes(btPulsantiera);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        var dictPulsantiere = new Dictionary("Pulsantiere 4x4", boardType: bt, description: "Condiviso");
        _dictionaryService.SeedData(dictPulsantiere);

        // Act — EdenXp NON ha board con btPulsantiera
        await _viewModel.LoadAsync(DeviceType.EdenXp);

        // Assert — condiviso NON visibile
        Assert.Empty(_viewModel.Dictionaries);
    }

    [Fact]
    public async Task LoadAsync_DedicatedDictionary_VisibleForMatchingDevice()
    {
        // Arrange — Dedicato (DT, BT)
        var btMadre = new BoardType("Madre Optimus", 17);
        _boardService.SeedBoardTypes(btMadre);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        var dictOptimus = new Dictionary("Optimus XP", DeviceType.OptimusXp, bt, "Dedicato");
        _dictionaryService.SeedData(dictOptimus);

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert
        Assert.Single(_viewModel.Dictionaries);
        Assert.Equal("Optimus XP", _viewModel.Dictionaries[0].Name);
    }

    [Fact]
    public async Task LoadAsync_DedicatedDictionary_NotVisibleForOtherDevice()
    {
        // Arrange — Dedicato (OptimusXp, BT)
        var btMadre = new BoardType("Madre Optimus", 17);
        _boardService.SeedBoardTypes(btMadre);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        var dictOptimus = new Dictionary("Optimus XP", DeviceType.OptimusXp, bt, "Dedicato");
        _dictionaryService.SeedData(dictOptimus);

        // Act — EdenXp non deve vedere dizionari di OptimusXp
        await _viewModel.LoadAsync(DeviceType.EdenXp);

        // Assert
        Assert.Empty(_viewModel.Dictionaries);
    }

    [Fact]
    public async Task LoadAsync_MixedSemantics_ShowsCorrectSubset()
    {
        // Arrange — scenario completo come il seeder
        var btMadreOpt = new BoardType("Madre Optimus", 17);
        var btPulsantiera = new BoardType("Pulsantiera 4x4", 4);
        var btMadreEden = new BoardType("Madre Eden", 18);
        _boardService.SeedBoardTypes(btMadreOpt, btPulsantiera, btMadreEden);
        var boardTypes = await _boardService.GetBoardTypesAsync();
        var btOpt = boardTypes[0];
        var btPuls = boardTypes[1];
        var btEden = boardTypes[2];

        // Board di OptimusXp: madre + pulsantiera
        await _boardService.AddAsync(new Board(DeviceType.OptimusXp, btOpt, "Madre #1", 1));
        await _boardService.AddAsync(new Board(DeviceType.OptimusXp, btPuls, "Tastiera 1", 1));

        // Dizionari di tutte e 3 le semantiche
        var dictStandard = new Dictionary("Standard", description: "Comune");
        var dictOptimus = new Dictionary("Optimus XP", DeviceType.OptimusXp, btOpt, "Dedicato");
        var dictPulsantiere = new Dictionary("Pulsantiere 4x4", boardType: btPuls, description: "Condiviso");
        var dictEden = new Dictionary("Eden XP", DeviceType.EdenXp, btEden, "Dedicato altro device");
        _dictionaryService.SeedData(dictStandard, dictOptimus, dictPulsantiere, dictEden);

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert — deve vedere Standard + Optimus XP + Pulsantiere 4x4, NON Eden XP
        Assert.Equal(3, _viewModel.Dictionaries.Count);
        var names = _viewModel.Dictionaries.Select(d => d.Name).ToList();
        Assert.Contains("Standard", names);
        Assert.Contains("Optimus XP", names);
        Assert.Contains("Pulsantiere 4x4", names);
        Assert.DoesNotContain("Eden XP", names);
    }

    [Fact]
    public async Task LoadAsync_DictionariesAreOrderedByName()
    {
        // Arrange — nomi non alfabetici
        var btMadre = new BoardType("Madre", 17);
        _boardService.SeedBoardTypes(btMadre);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        await _boardService.AddAsync(new Board(DeviceType.OptimusXp, bt, "Board 1", 1));

        var dictC = new Dictionary("Zeta", DeviceType.OptimusXp, bt, "Ultimo");
        var dictA = new Dictionary("Alfa", DeviceType.OptimusXp, bt, "Primo");
        var dictB = new Dictionary("Beta", boardType: bt, description: "Medio");
        _dictionaryService.SeedData(dictC, dictA, dictB);

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert — ordinati per nome
        Assert.Equal(3, _viewModel.Dictionaries.Count);
        Assert.Equal("Alfa", _viewModel.Dictionaries[0].Name);
        Assert.Equal("Beta", _viewModel.Dictionaries[1].Name);
        Assert.Equal("Zeta", _viewModel.Dictionaries[2].Name);
    }

    [Fact]
    public async Task LoadAsync_DictionaryItem_MapsProperties()
    {
        // Arrange
        var btMadre = new BoardType("Madre Optimus", 17);
        _boardService.SeedBoardTypes(btMadre);
        var bt = (await _boardService.GetBoardTypesAsync())[0];

        await _boardService.AddAsync(new Board(DeviceType.OptimusXp, bt, "Board 1", 1));

        var dict = new Dictionary("Optimus XP", DeviceType.OptimusXp, bt, "Test");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert — DictionaryItem mappa correttamente
        var item = Assert.Single(_viewModel.Dictionaries);
        Assert.True(item.Id > 0);
        Assert.Equal("Optimus XP", item.Name);
        Assert.Equal("Madre Optimus", item.BoardTypeName);
        Assert.Equal(0, item.VariableCount);
    }

    [Fact]
    public async Task LoadAsync_StandardDictionary_HasNullBoardTypeName()
    {
        // Arrange
        var dictStandard = new Dictionary("Standard", description: "Variabili comuni");
        _dictionaryService.SeedData(dictStandard);

        // Act
        await _viewModel.LoadAsync(DeviceType.Gradino);

        // Assert
        var item = Assert.Single(_viewModel.Dictionaries);
        Assert.Null(item.BoardTypeName);
    }

    [Fact]
    public async Task LoadAsync_ServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _dictionaryService.ExceptionToThrow = new InvalidOperationException("DB connection failed");

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert
        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("DB connection failed", _viewModel.ErrorMessage);
        Assert.Empty(_viewModel.Dictionaries);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingFalseAfterCompletion()
    {
        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert — IsLoading torna false sia in caso di successo
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingFalseAfterError()
    {
        // Arrange
        _boardService.ExceptionToThrow = new Exception("Errore");

        // Act
        await _viewModel.LoadAsync(DeviceType.OptimusXp);

        // Assert — IsLoading torna false anche in caso di errore
        Assert.False(_viewModel.IsLoading);
    }
}
#endif
