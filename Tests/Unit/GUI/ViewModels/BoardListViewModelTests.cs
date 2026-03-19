#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per BoardListViewModel.
/// </summary>
public class BoardListViewModelTests
{
    private readonly MockBoardService _boardService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly BoardListViewModel _viewModel;

    public BoardListViewModelTests()
    {
        _boardService = new MockBoardService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new BoardListViewModel(
            _boardService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_CallsGetAllAsync()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Contains("GetAllAsync", _boardService.MethodCalls);
    }

    [Fact]
    public async Task InitializeAsync_PopulatesBoardsList()
    {
        // Arrange
        var boardType = new BoardType("Madre", 17);
        _boardService.SeedBoardTypes(boardType);
        var bt = (await _boardService.GetBoardTypesAsync())[0];
        
        var board1 = new Board(DeviceType.Optimus, bt, "Board1", 1);
        var board2 = new Board(DeviceType.Eden, bt, "Board2", 2);
        await _boardService.AddAsync(board1);
        await _boardService.AddAsync(board2);
        _boardService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Equal(2, _viewModel.Boards.Count);
    }

    [Fact]
    public async Task InitializeAsync_ShowsSuccessMessage()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Contains(_messageService.Messages, m => 
            m.Message.Contains("Caricate") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _boardService.ExceptionToThrow = new Exception("Database error");

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Equal("Database error", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        _boardService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Empty(_boardService.MethodCalls);
    }

    [Fact]
    public void AddCommand_NavigatesToBoardEdit()
    {
        // Act
        _viewModel.AddCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.BoardEdit, _navigationService.LastNavigatedView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditCommand_NavigatesToBoardEdit_WithId()
    {
        // Arrange
        var item = new BoardListItem { Id = 42, Name = "TestBoard" };

        // Act
        _viewModel.EditCommand.Execute(item);

        // Assert
        Assert.Equal(ViewType.BoardEdit, _navigationService.LastNavigatedView);
        Assert.Equal(42, _navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditCommand_WithNull_DoesNotNavigate()
    {
        // Act
        _viewModel.EditCommand.Execute(null);

        // Assert
        Assert.Null(_navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task DeleteCommand_WithConfirmation_DeletesAndRefreshes()
    {
        // Arrange
        var boardType = new BoardType("Madre", 17);
        _boardService.SeedBoardTypes(boardType);
        var bt = (await _boardService.GetBoardTypesAsync())[0];
        var board = new Board(DeviceType.Optimus, bt, "ToDelete", 1);
        await _boardService.AddAsync(board);
        
        await _viewModel.InitializeAsync();
        _dialogService.ConfirmResult = DialogResult.Yes;
        _boardService.MethodCalls.Clear();

        var item = new BoardListItem { Id = 2, Name = "ToDelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.Contains("DeleteAsync:2", _boardService.MethodCalls);
    }

    [Fact]
    public async Task DeleteCommand_WithCancel_DoesNotDelete()
    {
        // Arrange
        _dialogService.ConfirmResult = DialogResult.No;
        var item = new BoardListItem { Id = 1, Name = "ToDelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.DoesNotContain(_boardService.MethodCalls, m => m.StartsWith("DeleteAsync"));
    }

    [Fact]
    public void GoBackCommand_CallsNavigationGoBack()
    {
        // Act
        _viewModel.GoBackCommand.Execute(null);

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
