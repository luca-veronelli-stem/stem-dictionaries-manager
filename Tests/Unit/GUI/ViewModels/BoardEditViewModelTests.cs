#if WINDOWS
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
    private readonly MockDeviceService _deviceService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly BoardEditViewModel _viewModel;

    public BoardEditViewModelTests()
    {
        _boardService = new MockBoardService();
        _deviceService = new MockDeviceService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new BoardEditViewModel(
            _boardService,
            _deviceService,
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
    public async Task InitializeAsync_New_PreFillsFirmwareType()
    {
        // Nessuna board seedata → max=0, next=1
        await _viewModel.InitializeAsync(null);

        Assert.Equal(1, _viewModel.FirmwareType);
    }

    [Fact]
    public async Task InitializeAsync_New_PreFillsFirmwareType_WithExistingBoards()
    {
        _boardService.SeedBoards(
            Board.Restore(1, 10, "A", 17, 1, null, false, null, 10),
            Board.Restore(2, 10, "B", 20, 2, null, false, null, 10));

        var vm = new BoardEditViewModel(
            _boardService, _deviceService, _navigationService,
            _dialogService, _messageService);
        await vm.InitializeAsync(null);

        Assert.Equal(21, vm.FirmwareType);
    }

    [Fact]
    public async Task InitializeAsync_New_SetsFirmwareTypeHint()
    {
        await _viewModel.InitializeAsync(null);

        Assert.NotNull(_viewModel.FirmwareTypeHint);
        Assert.Contains("1", _viewModel.FirmwareTypeHint);
    }

    [Fact]
    public async Task InitializeAsync_New_HasChangesIsFalse()
    {
        await _viewModel.InitializeAsync(null);

        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public async Task InitializeAsync_Edit_NoFirmwareTypeHint()
    {
        var board = new Board(10, "Existing", 17, 1, 10);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);

        Assert.Null(_viewModel.FirmwareTypeHint);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        var board = new Board(10, "Existing", 17, 1, 10);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);

        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Scheda", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_LoadsExistingData()
    {
        var board = new Board(3, "TestBoard", 18, 3, 3, "PN123");
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);

        Assert.Equal("TestBoard", _viewModel.Name);
        Assert.Equal(3, _viewModel.DeviceId);
        Assert.Equal(18, _viewModel.FirmwareType);
        Assert.Equal(3, _viewModel.BoardNumber);
        Assert.Equal("PN123", _viewModel.PartNumber);
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
    public async Task SaveCommand_WithEmptyName_ShowsWarning()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 1;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_messageService.Messages, m =>
            m.Severity == MessageSeverity.Warning && m.Message.Contains("Nome"));
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_WithInvalidFirmwareType_ShowsWarning()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.FirmwareType = 0;
        _viewModel.BoardNumber = 1;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_messageService.Messages, m =>
            m.Severity == MessageSeverity.Warning && m.Message.Contains("Firmware"));
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_WithInvalidBoardNumber_ShowsWarning()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 0;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_messageService.Messages, m =>
            m.Severity == MessageSeverity.Warning && m.Message.Contains("Numero Scheda"));
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_AlwaysCanExecute()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";

        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void IsNameInvalid_FalseBeforeFirstSave()
    {
        _viewModel.Name = "";

        Assert.False(_viewModel.IsNameInvalid);
    }

    [Fact]
    public async Task IsNameInvalid_TrueAfterSaveAttempt()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 1;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsNameInvalid);
    }

    [Fact]
    public async Task IsDeviceIdLocked_WhenPresetDeviceId()
    {
        await _viewModel.InitializeAsync(null, 3);

        Assert.True(_viewModel.IsDeviceIdLocked);
        Assert.Equal(3, _viewModel.DeviceId);
    }

    [Fact]
    public async Task IsDeviceIdLocked_FalseWithoutPreset()
    {
        await _viewModel.InitializeAsync(null);

        Assert.False(_viewModel.IsDeviceIdLocked);
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
    public void DeviceId_DefaultsToZero()
    {
        Assert.Equal(0, _viewModel.DeviceId);
    }

    [Fact]
    public void IsPrimary_DefaultFalse()
    {
        Assert.False(_viewModel.IsPrimary);
    }

    [Fact]
    public async Task InitializeAsync_WithPrimaryBoard_SetsIsPrimary()
    {
        var board = new Board(10, "Madre", 17, 1, 10, isPrimary: true);
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

    // === Test CancelCommand con HasChanges ===

    [Fact]
    public async Task CancelCommand_WithChanges_ShowsConfirmDialog()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c =>
            c.Type == "Confirm" && c.Message.Contains("annullare"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CancelCommand_WithChanges_UserDenies_StaysOnPage()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.No;

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c => c.Type == "Confirm");
        Assert.False(_navigationService.GoBackCalled);
    }

    // === Test DeleteBoardCommand ===

    [Fact]
    public async Task DeleteBoardCommand_ConfirmedYes_DeletesAndGoesBack()
    {
        var board = new Board(3, "Madre", 17, 1, 3);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.DeleteBoardCommand.ExecuteAsync(null);

        Assert.Contains(_boardService.MethodCalls, c => c == "DeleteAsync:1");
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteBoardCommand_ConfirmedNo_DoesNotDelete()
    {
        var board = new Board(3, "Madre", 17, 1, 3);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.No;

        await _viewModel.DeleteBoardCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_boardService.MethodCalls, c => c.StartsWith("DeleteAsync"));
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteBoardCommand_ServiceThrows_ShowsErrorDialog()
    {
        var board = new Board(3, "Madre", 17, 1, 3);
        await _boardService.AddAsync(board);

        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.Yes;
        _boardService.ExceptionToThrow = new Exception("FK constraint");

        await _viewModel.DeleteBoardCommand.ExecuteAsync(null);

        Assert.True(_dialogService.ShowErrorCalled);
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteBoardCommand_WhenNew_DoesNothing()
    {
        await _viewModel.InitializeAsync(null);

        await _viewModel.DeleteBoardCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_boardService.MethodCalls, c => c.StartsWith("DeleteAsync"));
        Assert.Empty(_dialogService.Calls);
    }
}
#endif
