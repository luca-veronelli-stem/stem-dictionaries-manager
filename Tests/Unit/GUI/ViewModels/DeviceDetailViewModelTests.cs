#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DeviceDetailViewModel (Domain v2).
/// Semantica: Standard (IsStandard) + Linked (Board→DictionaryId).
/// </summary>
public class DeviceDetailViewModelTests
{
    private readonly MockNavigationService _navigationService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockBoardService _boardService;
    private readonly MockDeviceService _deviceService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DeviceDetailViewModel _viewModel;

    public DeviceDetailViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _dictionaryService = new MockDictionaryService();
        _boardService = new MockBoardService();
        _deviceService = new MockDeviceService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();
        _deviceService.SeedDefaultDevices();

        _viewModel = new DeviceDetailViewModel(
            _navigationService,
            _dictionaryService,
            _boardService,
            _deviceService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public void Constructor_DefaultValues()
    {
        Assert.Null(_viewModel.DeviceId);
        Assert.Empty(_viewModel.DeviceName);
        Assert.Empty(_viewModel.Dictionaries);
        Assert.Empty(_viewModel.Boards);
        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_SetsDeviceTypeAndName()
    {
        await _viewModel.LoadAsync(9);

        Assert.Equal(9, _viewModel.DeviceId);
        Assert.Equal("Optimus-XP", _viewModel.DeviceName);
    }

    [Fact]
    public async Task LoadAsync_SherpaSlim_SetsCorrectName()
    {
        await _viewModel.LoadAsync(1);

        Assert.Equal("Sherpa Slim", _viewModel.DeviceName);
    }

    [Fact]
    public async Task LoadAsync_LoadsDictionaries()
    {
        // Arrange — board punta a un dizionario
        var dict = new Dictionary("Optimus XP", "Variabili Optimus XP");
        _dictionaryService.SeedData(dict);
        var seededDict = (await _dictionaryService.GetAllAsync())[0];

        var board = new Board(10, "Madre #1", 17, 1,
            dictionaryId: seededDict.Id);
        await _boardService.AddAsync(board);

        // Act
        await _viewModel.LoadAsync(10);

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
    public async Task EditDeviceCommand_NavigatesToDeviceEdit()
    {
        await _viewModel.LoadAsync(3);

        _viewModel.EditDeviceCommand.Execute(null);

        Assert.Equal(ViewType.DeviceEdit, _navigationService.LastNavigatedView);
        Assert.Equal(3, _navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditDeviceCommand_WhenNoDeviceId_DoesNotNavigate()
    {
        _viewModel.EditDeviceCommand.Execute(null);

        Assert.Empty(_navigationService.NavigationHistory);
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
    public void OpenDictionaryCommand_WithSelection_NavigatesToDictionaryEdit()
    {
        _viewModel.SelectedDictionary = new DictionaryItem(42, "Test Dict", "Specifico", 10);

        _viewModel.OpenDictionaryCommand.Execute(null);

        Assert.Equal(ViewType.DictionaryEdit, _navigationService.LastNavigatedView);
        Assert.Equal(42, _navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void OpenDictionaryCommand_StandardDictionary_NavigatesToDeviceVariables()
    {
        _viewModel.DeviceId = 3;
        _viewModel.SelectedDictionary = new DictionaryItem(1, "Standard", "Standard", 10)
            { IsStandard = true };

        _viewModel.OpenDictionaryCommand.Execute(null);

        Assert.Equal(ViewType.DeviceVariables, _navigationService.LastNavigatedView);
        Assert.Equal(3, _navigationService.LastParameter?.DeviceId);
    }

    [Theory]
    [InlineData(1, "Sherpa Slim")]
    [InlineData(2, "TopLift-M")]
    [InlineData(3, "Eden-XP")]
    [InlineData(4, "Gradino")]
    [InlineData(5, "Spyke")]
    [InlineData(6, "Spark")]
    [InlineData(7, "TopLift-A2")]
    [InlineData(8, "O3Z-Tech")]
    [InlineData(9, "Optimus-XP")]
    [InlineData(10, "R3L-XP")]
    [InlineData(11, "Eden-BS8")]
    public async Task DeviceName_MapsAllDeviceTypes(int deviceId, string expectedName)
    {
        await _viewModel.LoadAsync(deviceId);

        Assert.Equal(expectedName, _viewModel.DeviceName);
    }

    // === Test semantiche filtro dizionari ===

    [Fact]
    public async Task LoadAsync_StandardDictionary_VisibleForAnyDevice()
    {
        // Arrange — dizionario Standard (IsStandard=true)
        var dictStandard = new Dictionary("Standard", "Variabili comuni", true);
        _dictionaryService.SeedData(dictStandard);

        // Act — device senza board
        await _viewModel.LoadAsync(5);

        // Assert — Standard è sempre visibile (+ entry Comandi)
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        Assert.Single(realDicts);
        Assert.Equal("Standard", realDicts[0].Name);
        Assert.Single(_viewModel.Dictionaries, d => d.IsCommandsEntry);
    }

    [Fact]
    public async Task LoadAsync_LinkedDictionary_VisibleWhenBoardPointsToIt()
    {
        // Arrange — Board di OptimusXp punta a un dizionario
        var dict = new Dictionary("Pulsantiere 4x4", "Condiviso");
        _dictionaryService.SeedData(dict);
        var seededDict = (await _dictionaryService.GetAllAsync())[0];

        var board = new Board(10, "Tastiera 1", 4, 1,
            dictionaryId: seededDict.Id);
        await _boardService.AddAsync(board);

        // Act
        await _viewModel.LoadAsync(10);

        // Assert (+ entry Comandi)
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        Assert.Single(realDicts);
        Assert.Equal("Pulsantiere 4x4", realDicts[0].Name);
    }

    [Fact]
    public async Task LoadAsync_UnlinkedDictionary_NotVisibleWhenNoBoardPointsToIt()
    {
        // Arrange — dizionario non-standard, nessuna board lo referenzia
        var dict = new Dictionary("Pulsantiere 4x4", "Condiviso");
        _dictionaryService.SeedData(dict);

        // Act — EdenXp non ha board che puntano a quel dizionario
        await _viewModel.LoadAsync(3);

        // Assert (solo entry Comandi, nessun dizionario reale)
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        Assert.Empty(realDicts);
    }

    [Fact]
    public async Task LoadAsync_MixedSemantics_ShowsCorrectSubset()
    {
        // Arrange — Standard + 2 dizionari specifici + 1 di altro device
        var dictStandard = new Dictionary("Standard", "Comune", true);
        var dictOptimus = new Dictionary("Optimus XP", "Dedicato");
        var dictPulsantiere = new Dictionary("Pulsantiere 4x4", "Condiviso");
        var dictEden = new Dictionary("Eden XP", "Dedicato altro device");
        _dictionaryService.SeedData(dictStandard, dictOptimus, dictPulsantiere, dictEden);

        var allDicts = await _dictionaryService.GetAllAsync();
        var dictOptimusId = allDicts.First(d => d.Name == "Optimus XP").Id;
        var dictPulsantiereId = allDicts.First(d => d.Name == "Pulsantiere 4x4").Id;

        // Board di OptimusXp puntano a dictOptimus e dictPulsantiere
        await _boardService.AddAsync(new Board(10, "Madre #1", 17, 1,
            dictionaryId: dictOptimusId));
        await _boardService.AddAsync(new Board(10, "Tastiera 1", 4, 2,
            dictionaryId: dictPulsantiereId));

        // Act
        await _viewModel.LoadAsync(10);

        // Assert — Standard + Optimus XP + Pulsantiere 4x4 + Comandi, NOT Eden XP
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        Assert.Equal(3, realDicts.Count);
        var names = realDicts.Select(d => d.Name).ToList();
        Assert.Contains("Standard", names);
        Assert.Contains("Optimus XP", names);
        Assert.Contains("Pulsantiere 4x4", names);
        Assert.DoesNotContain("Eden XP", names);
    }

    [Fact]
    public async Task LoadAsync_DictionariesAreOrderedByName()
    {
        // Arrange
        var dictC = new Dictionary("Zeta", "Ultimo");
        var dictA = new Dictionary("Alfa", "Primo");
        var dictB = new Dictionary("Beta", "Medio");
        _dictionaryService.SeedData(dictC, dictA, dictB);

        var allDicts = await _dictionaryService.GetAllAsync();

        // Board di OptimusXp punta a tutti e 3
        foreach (var d in allDicts)
        {
            await _boardService.AddAsync(new Board(10,
                $"Board-{d.Name}", 17, allDicts.ToList().IndexOf(d) + 1,
                dictionaryId: d.Id));
        }

        // Act
        await _viewModel.LoadAsync(10);

        // Assert — ordinati per nome (Comandi in coda)
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        Assert.Equal(3, realDicts.Count);
        Assert.Equal("Alfa", realDicts[0].Name);
        Assert.Equal("Beta", realDicts[1].Name);
        Assert.Equal("Zeta", realDicts[2].Name);
        Assert.Equal("Comandi", _viewModel.Dictionaries.Last().Name);
    }

    [Fact]
    public async Task LoadAsync_DictionaryItem_MapsProperties()
    {
        // Arrange
        var dict = new Dictionary("Optimus XP", "Test");
        _dictionaryService.SeedData(dict);
        var seededDict = (await _dictionaryService.GetAllAsync())[0];

        await _boardService.AddAsync(new Board(10, "Board 1", 17, 1,
            dictionaryId: seededDict.Id));

        // Act
        await _viewModel.LoadAsync(10);

        // Assert (+ entry Comandi)
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        var item = Assert.Single(realDicts);
        Assert.True(item.Id > 0);
        Assert.Equal("Optimus XP", item.Name);
        Assert.Equal("Specifico", item.Semantic);
        Assert.Equal(0, item.VariableCount);
    }

    [Fact]
    public async Task LoadAsync_StandardDictionary_HasStandardSemantic()
    {
        // Arrange
        var dictStandard = new Dictionary("Standard", "Variabili comuni", true);
        _dictionaryService.SeedData(dictStandard);

        // Act
        await _viewModel.LoadAsync(4);

        // Assert (+ entry Comandi)
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        var item = Assert.Single(realDicts);
        Assert.Equal("Standard", item.Semantic);
    }

    [Fact]
    public async Task LoadAsync_StandardDictionary_HasIsStandardTrue()
    {
        // Arrange
        var dictStandard = new Dictionary("Standard", "Variabili comuni", true);
        _dictionaryService.SeedData(dictStandard);

        // Act
        await _viewModel.LoadAsync(4);

        // Assert
        var realDicts = _viewModel.Dictionaries.Where(d => !d.IsCommandsEntry).ToList();
        var item = Assert.Single(realDicts);
        Assert.True(item.IsStandard);
    }

    [Fact]
    public async Task LoadAsync_ServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _dictionaryService.ExceptionToThrow = new InvalidOperationException("DB connection failed");

        // Act
        await _viewModel.LoadAsync(10);

        // Assert
        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("DB connection failed", _viewModel.ErrorMessage);
        Assert.Empty(_viewModel.Dictionaries);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingFalseAfterCompletion()
    {
        // Act
        await _viewModel.LoadAsync(10);

        // Assert
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingFalseAfterError()
    {
        // Arrange
        _boardService.ExceptionToThrow = new Exception("Errore");

        // Act
        await _viewModel.LoadAsync(10);

        // Assert
        Assert.False(_viewModel.IsLoading);
    }

    // === Test sezione Schede (F5.2) ===

    [Fact]
    public async Task LoadAsync_PopulatesBoards()
    {
        // Arrange
        await _boardService.AddAsync(new Board(3, "Madre", 17, 1));
        await _boardService.AddAsync(new Board(3, "Pulsantiera", 4, 2));

        // Act
        await _viewModel.LoadAsync(3);

        // Assert
        Assert.Equal(2, _viewModel.Boards.Count);
    }

    [Fact]
    public async Task LoadAsync_BoardsAreOrderedByBoardNumber()
    {
        // Arrange — inseriti in ordine inverso
        await _boardService.AddAsync(new Board(7, "Rostro", 22, 4));
        await _boardService.AddAsync(new Board(7, "HMI", 20, 1));
        await _boardService.AddAsync(new Board(7, "Motore DX", 21, 2));

        // Act
        await _viewModel.LoadAsync(7);

        // Assert
        Assert.Equal(3, _viewModel.Boards.Count);
        Assert.Equal(1, _viewModel.Boards[0].BoardNumber);
        Assert.Equal(2, _viewModel.Boards[1].BoardNumber);
        Assert.Equal(4, _viewModel.Boards[2].BoardNumber);
    }

    [Fact]
    public async Task LoadAsync_BoardListItem_MapsProperties()
    {
        // Arrange
        var dict = new Dictionary("Eden-XP", "Dedicato");
        _dictionaryService.SeedData(dict);
        var seededDict = (await _dictionaryService.GetAllAsync())[0];

        await _boardService.AddAsync(new Board(3, "Madre", 17, 1,
            partNumber: "DIS0020477", isPrimary: true, dictionaryId: seededDict.Id));

        // Act
        await _viewModel.LoadAsync(3);

        // Assert
        var item = Assert.Single(_viewModel.Boards);
        Assert.Equal("Madre", item.Name);
        Assert.Equal(17, item.FirmwareType);
        Assert.Equal(1, item.BoardNumber);
        Assert.Equal("DIS0020477", item.PartNumber);
        Assert.True(item.IsPrimary);
        Assert.StartsWith("0x", item.ProtocolAddress);
    }

    [Fact]
    public async Task LoadAsync_OnlyShowsBoardsForSelectedDevice()
    {
        // Arrange — board di due device diversi
        await _boardService.AddAsync(new Board(3, "Madre Eden", 17, 1));
        await _boardService.AddAsync(new Board(7, "HMI Spark", 20, 1));

        // Act
        await _viewModel.LoadAsync(3);

        // Assert — solo Eden
        Assert.Single(_viewModel.Boards);
        Assert.Equal("Madre Eden", _viewModel.Boards[0].Name);
    }

    [Fact]
    public async Task AddBoardCommand_NavigatesToBoardEdit_WithDeviceType()
    {
        // Arrange
        await _viewModel.LoadAsync(3);

        // Act
        _viewModel.AddBoardCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.BoardEdit, _navigationService.LastNavigatedView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
        Assert.Equal(3, _navigationService.LastParameter?.DeviceId);
    }

    [Fact]
    public void AddBoardCommand_WithoutDeviceType_DoesNotNavigate()
    {
        // Act — deviceId è null (non ancora caricato)
        _viewModel.AddBoardCommand.Execute(null);

        // Assert
        Assert.Null(_navigationService.LastNavigatedView);
    }

    [Fact]
    public void EditBoardCommand_NavigatesToBoardEdit_WithEntityId()
    {
        // Arrange
        var item = new BoardListItem { Id = 42, Name = "Madre" };

        // Act
        _viewModel.EditBoardCommand.Execute(item);

        // Assert
        Assert.Equal(ViewType.BoardEdit, _navigationService.LastNavigatedView);
        Assert.Equal(42, _navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditBoardCommand_WithNull_DoesNotNavigate()
    {
        // Act
        _viewModel.EditBoardCommand.Execute(null);

        // Assert
        Assert.Null(_navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task ReloadBoardsAsync_RefreshesOnlyBoards()
    {
        // Arrange — carica iniziale
        await _boardService.AddAsync(new Board(3, "Madre", 17, 1));
        await _viewModel.LoadAsync(3);
        Assert.Single(_viewModel.Boards);

        // Aggiungi una board dopo il load iniziale
        await _boardService.AddAsync(new Board(3, "Pulsantiera", 4, 2));

        // Act
        await _viewModel.ReloadBoardsAsync();

        // Assert — boards aggiornate, dizionari non toccati
        Assert.Equal(2, _viewModel.Boards.Count);
    }

    [Fact]
    public async Task ReloadBoardsAsync_WithoutDeviceType_DoesNothing()
    {
        // Act — deviceId è null (non caricato)
        await _viewModel.ReloadBoardsAsync();

        // Assert
        Assert.Empty(_viewModel.Boards);
        Assert.DoesNotContain(_boardService.MethodCalls,
            c => c.StartsWith("GetByDeviceIdAsync"));
    }
}
#endif
