#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per BoardEditViewModel (Domain v2).
/// FirmwareType diretto, DictionaryId opzionale, nessun BoardType.
/// </summary>
public class BoardEditViewModelTests
{
    private readonly MockBoardService _boardService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly BoardEditViewModel _viewModel;

    public BoardEditViewModelTests()
    {
        _boardService = new MockBoardService();
        _dictionaryService = new MockDictionaryService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new BoardEditViewModel(
            _boardService,
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        await _viewModel.InitializeAsync(null);

        Assert.True(_viewModel.IsNew);
        Assert.Equal("Nuova Scheda", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        var board = new Board(DeviceType.OptimusXp, "Existing", 17, 1);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);

        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Scheda", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_LoadsDictionaries()
    {
        _dictionaryService.SeedData(
            new Dictionary("Dict1"),
            new Dictionary("Dict2"));

        await _viewModel.InitializeAsync(null);

        Assert.Equal(2, _viewModel.AvailableDictionaries.Count);
        Assert.Contains(_viewModel.AvailableDictionaries, d => d.Name == "Dict1");
        Assert.Contains(_viewModel.AvailableDictionaries, d => d.Name == "Dict2");
    }

    [Fact]
    public async Task InitializeAsync_LoadsExistingData()
    {
        var board = new Board(DeviceType.EdenXp, "TestBoard", 18, 3, "PN123");
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);

        Assert.Equal("TestBoard", _viewModel.Name);
        Assert.Equal(DeviceType.EdenXp, _viewModel.SelectedDeviceType);
        Assert.Equal(18, _viewModel.FirmwareType);
        Assert.Equal(3, _viewModel.BoardNumber);
        Assert.Equal("PN123", _viewModel.PartNumber);
    }

    [Fact]
    public async Task InitializeAsync_WithDictionaryId_SelectsDictionary()
    {
        _dictionaryService.SeedData(new Dictionary("TestDict"));
        var allDicts = await _dictionaryService.GetAllAsync();
        var dictId = allDicts[0].Id;

        var board = new Board(DeviceType.OptimusXp, "Madre", 17, 1, dictionaryId: dictId);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);

        Assert.NotNull(_viewModel.SelectedDictionary);
        Assert.Equal("TestDict", _viewModel.SelectedDictionary!.Name);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
        await _viewModel.InitializeAsync(999);

        Assert.True(_dialogService.ShowErrorCalled);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        await _viewModel.InitializeAsync(null);
        _boardService.MethodCalls.Clear();

        await _viewModel.InitializeAsync(null);

        Assert.Empty(_boardService.MethodCalls);
    }

    [Fact]
    public async Task SaveCommand_CannotExecute_WhenNameEmpty()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";

        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_CanExecute_WhenNameSet()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";

        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewBoard";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 1;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_boardService.MethodCalls, m => m.StartsWith("AddAsync:NewBoard"));
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.FirmwareType = 17;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.FirmwareType = 17;
        _boardService.ExceptionToThrow = new Exception("Save failed");

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_dialogService.ShowErrorCalled);
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CancelCommand_WithNoChanges_GoesBack()
    {
        await _viewModel.InitializeAsync(null);

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public void DeviceTypes_ContainsAllValues()
    {
        Assert.Equal(Enum.GetValues<DeviceType>().Length, _viewModel.DeviceTypes.Count);
    }

    [Fact]
    public void IsPrimary_DefaultFalse()
    {
        Assert.False(_viewModel.IsPrimary);
    }

    [Fact]
    public async Task InitializeAsync_WithPrimaryBoard_SetsIsPrimary()
    {
        var board = new Board(DeviceType.OptimusXp, "Madre", 17, 1, isPrimary: true);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);

        Assert.True(_viewModel.IsPrimary);
    }

    [Fact]
    public async Task SaveCommand_NewBoardWithIsPrimary_PassesToService()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Principale";
        _viewModel.FirmwareType = 17;
        _viewModel.IsPrimary = true;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_boardService.MethodCalls, c => c == "AddAsync:Principale");
    }
}
#endif
