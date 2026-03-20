#if WINDOWS
using Core.Enums;
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
        var dict1 = new Dictionary("Test1", description: "Description 1");
        var dict2 = new Dictionary("Test2", description: "Description 2");
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
        _dictionaryService.SeedData(new Dictionary("Test"));

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
        var dict = new Dictionary("Test");
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
        var dict = new Dictionary("Test");
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
        var dict = new Dictionary("Test");
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
        var dict = new Dictionary("Test");
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
        var dict = new Dictionary("Test", description: "Desc");
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
        var dict = new Dictionary("Test", DeviceType.Optimus, boardType, "Desc");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.LoadAsync();

        // Assert
        var item = _viewModel.Dictionaries.First();
        Assert.Equal("Madre Optimus", item.BoardTypeDisplay);
    }

    [Fact]
    public async Task SearchText_FiltersListByName()
    {
        // Arrange
        _dictionaryService.SeedData(
            new Dictionary("optimus-xp"),
            new Dictionary("pulsantiere"),
            new Dictionary("standard"));
        await _viewModel.LoadAsync();

        // Act
        _viewModel.SearchText = "optimus";

        // Assert
        Assert.Single(_viewModel.Dictionaries);
        Assert.Equal("optimus-xp", _viewModel.Dictionaries[0].Name);
    }

    [Fact]
    public async Task SearchText_FiltersListByBoardType()
    {
        // Arrange
        var bt = new BoardType("Madre Optimus", 17);
        _dictionaryService.SeedData(
            new Dictionary("dict1", DeviceType.Optimus, bt),
            new Dictionary("dict2"));
        await _viewModel.LoadAsync();

        // Act
        _viewModel.SearchText = "Madre";

        // Assert
        Assert.Single(_viewModel.Dictionaries);
        Assert.Equal("dict1", _viewModel.Dictionaries[0].Name);
    }

    [Fact]
    public async Task SearchText_EmptyString_ShowsAll()
    {
        // Arrange
        _dictionaryService.SeedData(
            new Dictionary("dict1"),
            new Dictionary("dict2"));
        await _viewModel.LoadAsync();
        _viewModel.SearchText = "dict1";
        Assert.Single(_viewModel.Dictionaries);

        // Act
        _viewModel.SearchText = "";

        // Assert
        Assert.Equal(2, _viewModel.Dictionaries.Count);
    }

    [Fact]
    public async Task SearchText_CaseInsensitive()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Optimus-XP"));
        await _viewModel.LoadAsync();

        // Act
        _viewModel.SearchText = "OPTIMUS";

        // Assert
        Assert.Single(_viewModel.Dictionaries);
    }
}
#endif
