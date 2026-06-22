#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

namespace Tests.Integration.Scenarios;

/// <summary>
/// Integration test per il flusso completo di gestione schede.
/// Testa BR-005 (max 1 primary per device), BR-008 (BoardNumber 1..63).
/// </summary>
public class BoardEditFlowTests
{
    private readonly MockBoardService _boardService;
    private readonly MockDeviceService _deviceService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly BoardEditViewModel _viewModel;

    public BoardEditFlowTests()
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
            _messageService,
            NullLogger<BoardEditViewModel>.Instance);
    }

    #region Create Board Tests

    [Fact]
    public async Task CreateBoard_WithValidData_SavesAndNavigatesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, presetDeviceId: 1);

        _viewModel.Name = "Madre";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 1;
        _viewModel.PartNumber = "DIS0020477";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_boardService.MethodCalls, m => m.StartsWith("AddAsync:Madre"));
        Assert.True(_navigationService.GoBackCalled);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task CreateBoard_AsPrimary_WhenNoPrimaryExists_Succeeds()
    {
        // Arrange - nessuna board esistente
        await _viewModel.InitializeAsync(null, presetDeviceId: 1);

        _viewModel.Name = "Madre";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 1;
        _viewModel.IsPrimary = true;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_boardService.MethodCalls, m => m.StartsWith("AddAsync:Madre"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CreateBoard_AsPrimary_WhenPrimaryExists_ShowsError()
    {
        // Arrange - esiste già una primary per questo device
        var existingPrimary = Board.Restore(1, 1, "Madre", 17, 1, null, isPrimary: true, null, machineCode: 1);
        _boardService.SeedBoards(existingPrimary);

        await _viewModel.InitializeAsync(null, presetDeviceId: 1);

        // Tutti i campi validi per passare la validazione
        _viewModel.Name = "Pulsantiera";
        _viewModel.FirmwareType = 4;
        _viewModel.BoardNumber = 2;
        _viewModel.IsPrimary = true; // BR-005 violation

        // Imposta l'eccezione DOPO l'inizializzazione, prima del save
        _boardService.ExceptionToThrow = new InvalidOperationException(
            "Esiste già una scheda primaria per il dispositivo 'Eden-XP'.");

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - l'errore viene mostrato nel dialog, non nel message service
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CreateBoard_WithValidData_NoDict_Succeeds()
    {
        // Arrange — board senza dizionario (il link avviene dal DictionaryEdit)
        await _viewModel.InitializeAsync(null, presetDeviceId: 1);

        _viewModel.Name = "Madre";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 1;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_boardService.MethodCalls, m => m.StartsWith("AddAsync:Madre"));
    }

    [Fact]
    public async Task CreateBoard_WithoutDictionary_SucceedsForPeripheralWithoutVars()
    {
        // Arrange - board senza dizionario (es. SPARK Motore DX)
        await _viewModel.InitializeAsync(null, presetDeviceId: 1);

        _viewModel.Name = "Motore DX";
        _viewModel.FirmwareType = 21;
        _viewModel.BoardNumber = 2;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_boardService.MethodCalls, m => m.StartsWith("AddAsync:Motore DX"));
        Assert.True(_navigationService.GoBackCalled);
    }

    #endregion

    #region Edit Board Tests

    [Fact]
    public async Task EditBoard_LoadsExistingData()
    {
        // Arrange
        var existingBoard = Board.Restore(1, 1, "Madre", 17, 1, "DIS0020477", isPrimary: true, dictionaryId: 1, machineCode: 1);
        _boardService.SeedBoards(existingBoard);

        // Act
        await _viewModel.InitializeAsync(boardId: 1);

        // Assert
        Assert.Equal("Madre", _viewModel.Name);
        Assert.Equal(17, _viewModel.FirmwareType);
        Assert.Equal(1, _viewModel.BoardNumber);
        Assert.Equal("DIS0020477", _viewModel.PartNumber);
        Assert.True(_viewModel.IsPrimary);
        Assert.False(_viewModel.IsNew);
    }

    [Fact]
    public async Task EditBoard_ChangePrimary_WhenAnotherExists_ShowsError()
    {
        // Arrange
        var existingPrimary = Board.Restore(1, 1, "Madre", 17, 1, null, isPrimary: true, null, machineCode: 1);
        var editingBoard = Board.Restore(2, 1, "Pulsantiera", 4, 2, null, isPrimary: false, null, machineCode: 1);
        _boardService.SeedBoards(existingPrimary, editingBoard);

        await _viewModel.InitializeAsync(boardId: 2);
        _viewModel.IsPrimary = true; // BR-005 violation

        // Imposta l'eccezione DOPO l'inizializzazione, prima del save
        _boardService.ExceptionToThrow = new InvalidOperationException(
            "Esiste già una scheda primaria per il dispositivo 'Eden-XP'.");

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - l'errore viene mostrato nel dialog
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.False(_navigationService.GoBackCalled);
    }

    #endregion

    #region Delete Board Tests

    [Fact]
    public async Task DeleteBoard_WithConfirmation_DeletesAndNavigatesBack()
    {
        // Arrange
        var existingBoard = Board.Restore(1, 1, "Madre", 17, 1, null, isPrimary: false, null, machineCode: 1);
        _boardService.SeedBoards(existingBoard);
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.InitializeAsync(boardId: 1);

        // Act
        await _viewModel.DeleteBoardCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_boardService.MethodCalls, m => m == "DeleteAsync:1");
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteBoard_WithCancellation_DoesNotDelete()
    {
        // Arrange
        var existingBoard = Board.Restore(1, 1, "Madre", 17, 1, null, isPrimary: false, null, machineCode: 1);
        _boardService.SeedBoards(existingBoard);
        _dialogService.ConfirmResult = DialogResult.No;

        await _viewModel.InitializeAsync(boardId: 1);

        // Act
        await _viewModel.DeleteBoardCommand.ExecuteAsync(null);

        // Assert
        Assert.DoesNotContain(_boardService.MethodCalls, m => m.StartsWith("DeleteAsync"));
        Assert.False(_navigationService.GoBackCalled);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task BoardNumber_Zero_ShowsValidationError()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, presetDeviceId: 1);

        _viewModel.Name = "Test";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 0; // BR-008 violation

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.IsBoardNumberInvalid);
        Assert.DoesNotContain(_boardService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task BoardNumber_Over63_ShowsValidationError()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, presetDeviceId: 1);

        _viewModel.Name = "Test";
        _viewModel.FirmwareType = 17;
        _viewModel.BoardNumber = 64; // BR-008 violation

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.IsBoardNumberInvalid);
        Assert.DoesNotContain(_boardService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task CancelWithChanges_ShowsConfirmDialog()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, presetDeviceId: 1);
        _viewModel.Name = "Test"; // HasChanges = true (ora funziona!)
        _dialogService.ConfirmResult = DialogResult.Yes;

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.HasChanges);
        Assert.True(_dialogService.ShowConfirmCalled);
    }

    #endregion
}
#endif
