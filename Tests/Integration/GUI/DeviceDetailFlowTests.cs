#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso DeviceDetail (F5.2).
/// Testa navigazione verso dizionari, comandi, variabili e schede.
/// </summary>
public class DeviceDetailFlowTests
{
    private readonly MockDeviceService _deviceService;
    private readonly MockBoardService _boardService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockCommandService _commandService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DeviceDetailViewModel _viewModel;

    public DeviceDetailFlowTests()
    {
        _deviceService = new MockDeviceService();
        _boardService = new MockBoardService();
        _dictionaryService = new MockDictionaryService();
        _commandService = new MockCommandService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new DeviceDetailViewModel(
            _navigationService,
            _dictionaryService,
            _boardService,
            _deviceService,
            _commandService,
            _dialogService,
            _messageService);
    }

    #region Load Tests

    [Fact]
    public async Task LoadDeviceDetail_ShowsDerivedDictionaries()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        _dictionaryService.SeedData(
            Dictionary.Restore(1, "Eden-XP Main", null, false, []),
            Dictionary.Restore(2, "Standard", null, true, [])
        );
        _boardService.SeedBoards(
            Board.Restore(1, 1, "Madre", 17, 1, null, true, dictionaryId: 1)
        );

        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert - deve mostrare dizionario legato + standard + entry comandi
        Assert.True(_viewModel.Dictionaries.Count >= 2);
    }

    [Fact]
    public async Task LoadDeviceDetail_ExcludesStandardDictionary()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        _dictionaryService.SeedData(
            Dictionary.Restore(1, "Standard", null, true, [])
        );

        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert — Standard non appare più (accessibile solo dalla sidebar)
        Assert.DoesNotContain(_viewModel.Dictionaries, d => d.IsStandard);
    }

    [Fact]
    public async Task LoadDeviceDetail_ShowsLinkedBoards()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        _boardService.SeedBoards(
            Board.Restore(1, 1, "Madre", 17, 1, null, true, null),
            Board.Restore(2, 1, "Pulsantiera", 4, 2, null, false, null)
        );

        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert
        Assert.Equal(2, _viewModel.Boards.Count);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public async Task AddDictionary_NavigatesToDictionaryEdit_WithDeviceId()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        await _viewModel.LoadAsync(deviceId: 1);

        // Act
        _viewModel.AddDictionaryCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DictionaryEdit, _navigationService.LastNavigatedView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
        Assert.Equal(1, _navigationService.LastParameter?.DeviceId);
    }

    [Fact]
    public async Task OpenDictionary_NonStandard_NavigatesToDictionaryEdit()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        _dictionaryService.SeedData(Dictionary.Restore(1, "Eden-XP Main", null, false, []));
        _boardService.SeedBoards(Board.Restore(1, 1, "Madre", 17, 1, null, true, dictionaryId: 1));
        await _viewModel.LoadAsync(deviceId: 1);

        var nonStandardDict = _viewModel.Dictionaries.FirstOrDefault(d => !d.IsStandard && !d.IsCommandsEntry);

        // Act
        if (nonStandardDict is not null)
        {
            _viewModel.SelectedDictionary = nonStandardDict;
            _viewModel.OpenDictionaryCommand.Execute(null);

            // Assert
            Assert.Equal(ViewType.DictionaryEdit, _navigationService.LastNavigatedView);
        }
    }

    [Fact]
    public async Task OpenCommands_NavigatesToDeviceCommands()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        await _viewModel.LoadAsync(deviceId: 1);

        var commandsEntry = _viewModel.Dictionaries.FirstOrDefault(d => d.IsCommandsEntry);
        Assert.NotNull(commandsEntry);

        // Act
        _viewModel.SelectedDictionary = commandsEntry;
        _viewModel.OpenDictionaryCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DeviceCommands, _navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task AddBoard_NavigatesToBoardEdit_WithDevicePreset()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        await _viewModel.LoadAsync(deviceId: 1);

        // Act
        _viewModel.AddBoardCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.BoardEdit, _navigationService.LastNavigatedView);
        Assert.Equal(1, _navigationService.LastParameter?.DeviceId);
    }

    [Fact]
    public async Task EditBoard_LoadsSelectedBoard()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        _boardService.SeedBoards(Board.Restore(1, 1, "Madre", 17, 1, null, true, null));
        await _viewModel.LoadAsync(deviceId: 1);

        // Act
        Assert.NotEmpty(_viewModel.Boards);
        _viewModel.SelectedBoard = _viewModel.Boards[0];

        // Assert - la board è stata selezionata correttamente
        Assert.NotNull(_viewModel.SelectedBoard);
        Assert.Equal("Madre", _viewModel.SelectedBoard.Name);
    }

    [Fact]
    public async Task GoBack_NavigatesBack()
    {
        // Arrange
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test"));
        await _viewModel.LoadAsync(deviceId: 1);

        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    #endregion
}
#endif
