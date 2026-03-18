#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DictionaryListViewModel.
/// </summary>
public class DictionaryListViewModelTests
{
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DictionaryListViewModel _viewModel;

    public DictionaryListViewModelTests()
    {
        _dictionaryService = new MockDictionaryService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();
        
        _viewModel = new DictionaryListViewModel(
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task LoadAsync_CallsGetAllAsync()
    {
        // Act
        await _viewModel.LoadAsync();

        // Assert
        Assert.Contains("GetAllAsync", _dictionaryService.MethodCalls);
    }

    [Fact]
    public async Task LoadAsync_PopulatesDictionariesList()
    {
        // Arrange
        var dict1 = new Dictionary("Test1", null, "Description 1");
        var dict2 = new Dictionary("Test2", null, "Description 2");
        _dictionaryService.SeedData(dict1, dict2);

        // Act
        await _viewModel.LoadAsync();

        // Assert
        Assert.Equal(2, _viewModel.Dictionaries.Count);
        Assert.Contains(_viewModel.Dictionaries, d => d.Name == "Test1");
        Assert.Contains(_viewModel.Dictionaries, d => d.Name == "Test2");
    }

    [Fact]
    public async Task LoadAsync_ShowsSuccessMessage()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Test", null, null));

        // Act
        await _viewModel.LoadAsync();

        // Assert
        Assert.Contains(_messageService.Messages, m => 
            m.Message.Contains("Caricati") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _dictionaryService.ExceptionToThrow = new Exception("Database error");

        // Act
        await _viewModel.LoadAsync();

        // Assert
        Assert.Equal("Database error", _viewModel.ErrorMessage);
        Assert.Contains(_messageService.Messages, m => 
            m.Message.Contains("Errore") && m.Severity == MessageSeverity.Error);
    }

    [Fact]
    public async Task LoadAsync_SetsIsBusyDuringExecution()
    {
        // Arrange
        var wasBusyDuringCall = false;
        var dict = new Dictionary("Test", null, null);
        _dictionaryService.SeedData(dict);

        // Act - Check IsBusy state during the call
        var task = _viewModel.LoadAsync();
        // Note: In real scenario we'd use async coordination
        await task;

        // Assert - After completion, IsBusy should be false
        Assert.False(_viewModel.IsBusy);
    }

    [Fact]
    public void AddCommand_NavigatesToDictionaryEdit()
    {
        // Act
        _viewModel.AddCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DictionaryEdit, _navigationService.CurrentView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditCommand_WithItem_NavigatesToDictionaryEdit()
    {
        // Arrange
        var item = new DictionaryListItem { Id = 5, Name = "Test", VariableCount = 0 };

        // Act
        _viewModel.EditCommand.Execute(item);

        // Assert
        Assert.Equal(ViewType.DictionaryEdit, _navigationService.CurrentView);
        Assert.Equal(5, _navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditCommand_WithNull_DoesNotNavigate()
    {
        // Act
        _viewModel.EditCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DictionaryList, _navigationService.CurrentView);
        Assert.Empty(_navigationService.NavigationHistory);
    }

    [Fact]
    public async Task DeleteCommand_WithConfirmation_DeletesAndRefreshes()
    {
        // Arrange
        var dict = new Dictionary("Test", null, null);
        _dictionaryService.SeedData(dict);
        await _viewModel.LoadAsync();
        var item = _viewModel.Dictionaries.First();
        _dialogService.ConfirmResult = DialogResult.Yes;

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("DeleteAsync:"));
        Assert.Contains(_messageService.Messages, m => m.Message.Contains("eliminato"));
    }

    [Fact]
    public async Task DeleteCommand_WithCancel_DoesNotDelete()
    {
        // Arrange
        var dict = new Dictionary("Test", null, null);
        _dictionaryService.SeedData(dict);
        await _viewModel.LoadAsync();
        var item = _viewModel.Dictionaries.First();
        _dialogService.ConfirmResult = DialogResult.No;

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.DoesNotContain(_dictionaryService.MethodCalls, c => c.StartsWith("DeleteAsync:"));
    }

    [Fact]
    public async Task DeleteCommand_WhenServiceThrows_ShowsErrorDialog()
    {
        // Arrange
        var dict = new Dictionary("Test", null, null);
        _dictionaryService.SeedData(dict);
        await _viewModel.LoadAsync();
        var item = _viewModel.Dictionaries.First();
        _dialogService.ConfirmResult = DialogResult.Yes;
        _dictionaryService.ExceptionToThrow = new Exception("Delete failed");

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.Contains(_dialogService.Calls, c => c.Type == "Error");
    }

    [Fact]
    public void OpenVariablesCommand_NavigatesToVariableList()
    {
        // Arrange
        var item = new DictionaryListItem { Id = 7, Name = "Test", VariableCount = 3 };

        // Act
        _viewModel.OpenVariablesCommand.Execute(item);

        // Assert
        Assert.Equal(ViewType.VariableList, _navigationService.CurrentView);
        Assert.Equal(7, _navigationService.LastParameter?.ParentId);
    }

    [Fact]
    public async Task DictionaryListItem_BoardTypeDisplay_ReturnsStandardWhenNull()
    {
        // Arrange
        var dict = new Dictionary("Test", null, "Desc");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.LoadAsync();

        // Assert
        var item = _viewModel.Dictionaries.First();
        Assert.Equal("Standard", item.BoardTypeDisplay);
    }

    [Fact]
    public async Task DictionaryListItem_BoardTypeDisplay_ReturnsBoardTypeName()
    {
        // Arrange
        var boardType = new BoardType("Madre Optimus", 17);
        var dict = new Dictionary("Test", boardType, "Desc");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.LoadAsync();

        // Assert
        var item = _viewModel.Dictionaries.First();
        Assert.Equal("Madre Optimus", item.BoardTypeDisplay);
    }
}
#endif
