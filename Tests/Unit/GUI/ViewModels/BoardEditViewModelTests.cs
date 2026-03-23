#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per BoardEditViewModel.
/// </summary>
public class BoardEditViewModelTests
{
    private readonly MockBoardService _boardService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly BoardEditViewModel _viewModel;

    public BoardEditViewModelTests()
    {
        _boardService = new MockBoardService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new BoardEditViewModel(
            _boardService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));

        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.True(_viewModel.IsNew);
        Assert.Equal("Nuova Scheda", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        // Arrange
        var boardType = new BoardType("Madre", 17);
        _boardService.SeedBoardTypes(boardType);
        var bt = (await _boardService.GetBoardTypesAsync())[0];
        var board = new Board(DeviceType.OptimusXp, bt, "Existing", 1);
        await _boardService.AddAsync(board);

        // Act
        await _viewModel.InitializeAsync(2);

        // Assert
        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Scheda", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_LoadsBoardTypes()
    {
        // Arrange
        _boardService.SeedBoardTypes(
            new BoardType("Madre", 17),
            new BoardType("Pulsantiera", 4));

        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.Equal(2, _viewModel.AvailableBoardTypes.Count);
        Assert.Contains(_viewModel.AvailableBoardTypes, bt => bt.Name == "Madre");
        Assert.Contains(_viewModel.AvailableBoardTypes, bt => bt.Name == "Pulsantiera");
    }

    [Fact]
    public async Task InitializeAsync_LoadsExistingData()
    {
        // Arrange
        var boardType = new BoardType("Madre", 17);
        _boardService.SeedBoardTypes(boardType);
        var bt = (await _boardService.GetBoardTypesAsync())[0];
        var board = new Board(DeviceType.EdenXp, bt, "TestBoard", 3, "PN123");
        await _boardService.AddAsync(board);

        // Act
        await _viewModel.InitializeAsync(2);

        // Assert
        Assert.Equal("TestBoard", _viewModel.Name);
        Assert.Equal(DeviceType.EdenXp, _viewModel.SelectedDeviceType);
        Assert.Equal(3, _viewModel.BoardNumber);
        Assert.Equal("PN123", _viewModel.PartNumber);
        Assert.NotNull(_viewModel.SelectedBoardType);
        Assert.Equal("Madre", _viewModel.SelectedBoardType!.Name);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));

        // Act
        await _viewModel.InitializeAsync(999);

        // Assert
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);
        _boardService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.Empty(_boardService.MethodCalls);
    }

    [Fact]
    public async Task SaveCommand_CannotExecute_WhenNameEmpty()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";
        _viewModel.SelectedBoardType = _viewModel.AvailableBoardTypes[0];

        // Assert
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_CannotExecute_WhenBoardTypeNull()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.SelectedBoardType = null;

        // Assert
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_CanExecute_WhenValid()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.SelectedBoardType = _viewModel.AvailableBoardTypes[0];

        // Assert
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewBoard";
        _viewModel.SelectedBoardType = _viewModel.AvailableBoardTypes[0];
        _viewModel.BoardNumber = 1;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_boardService.MethodCalls, m => m.StartsWith("AddAsync:NewBoard"));
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.SelectedBoardType = _viewModel.AvailableBoardTypes[0];

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestBoard";
        _viewModel.SelectedBoardType = _viewModel.AvailableBoardTypes[0];
        _boardService.ExceptionToThrow = new Exception("Save failed");

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CancelCommand_WithNoChanges_GoesBack()
    {
        // Arrange
        _boardService.SeedBoardTypes(new BoardType("Madre", 17));
        await _viewModel.InitializeAsync(null);

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public void DeviceTypes_ContainsAllValues()
    {
        // Assert
        Assert.Equal(Enum.GetValues<DeviceType>().Length, _viewModel.DeviceTypes.Count);
    }
}
#endif
