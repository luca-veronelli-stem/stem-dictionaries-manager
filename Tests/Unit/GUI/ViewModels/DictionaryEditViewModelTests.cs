#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DictionaryEditViewModel.
/// </summary>
public class DictionaryEditViewModelTests
{
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockBoardService _boardService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DictionaryEditViewModel _viewModel;

    public DictionaryEditViewModelTests()
    {
        _dictionaryService = new MockDictionaryService();
        _boardService = new MockBoardService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new DictionaryEditViewModel(
            _dictionaryService,
            _boardService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.True(_viewModel.IsNew);
        Assert.Equal("Nuovo Dizionario", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        // Arrange
        var dict = new Dictionary("Existing", description: "Desc");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Dizionario", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_LoadsBoardTypes()
    {
        // Arrange
        _boardService.SeedBoardTypes(
            new BoardType("Type1", 1),
            new BoardType("Type2", 2));

        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.Equal(2, _viewModel.AvailableBoardTypes.Count);
        Assert.Contains(_viewModel.AvailableBoardTypes, bt => bt.Name == "Type1");
    }

    [Fact]
    public async Task InitializeAsync_WithId_LoadsExistingData()
    {
        // Arrange
        var dict = Dictionary.Restore(1, "TestDict", null, null, "TestDesc", []);
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Equal("TestDict", _viewModel.Name);
        Assert.Equal("TestDesc", _viewModel.Description);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
        // Act
        await _viewModel.InitializeAsync(999);

        // Assert
        Assert.Contains(_dialogService.Calls, c => c.Type == "Error" && c.Message.Contains("non trovato"));
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        // Act
        await _viewModel.InitializeAsync(null);
        _boardService.MethodCalls.Clear();
        await _viewModel.InitializeAsync(null); // Second call

        // Assert - GetBoardTypesAsync should not be called again
        Assert.DoesNotContain("GetBoardTypesAsync", _boardService.MethodCalls);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _boardService.ExceptionToThrow = new Exception("Service error");

        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.Equal("Service error", _viewModel.ErrorMessage);
    }

    [Fact]
    public void CanChangeDeviceAndBoardType_IsTrue_WhenNew()
    {
        // Assert (default state is new)
        Assert.True(_viewModel.CanChangeDeviceAndBoardType);
    }

    [Fact]
    public async Task CanChangeDeviceAndBoardType_IsFalse_WhenEditing()
    {
        // Arrange
        var dict = new Dictionary("Existing");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.False(_viewModel.CanChangeDeviceAndBoardType);
    }

    [Fact]
    public async Task SaveCommand_CannotExecute_WhenNameEmpty()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = string.Empty;

        // Assert
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_CanExecute_WhenNameSet()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Test";

        // Assert
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _viewModel.Description = "NewDesc";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("AddAsync:"));
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_CallsUpdateAsync()
    {
        // Arrange
        var dict = new Dictionary("Existing");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);
        _viewModel.Name = "Updated";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("UpdateAsync:"));
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _navigationService.NavigateTo(ViewType.DictionaryEdit); // Simulate navigation

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m =>
            m.Message.Contains("creato") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _dictionaryService.ExceptionToThrow = new Exception("Save failed");

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_dialogService.Calls, c => c.Type == "Error");
    }

    [Fact]
    public async Task OnNameChanged_SetsHasChanges()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        // Act
        _viewModel.Name = "Changed";

        // Assert
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task OnDescriptionChanged_SetsHasChanges()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        // Act
        _viewModel.Description = "Changed description";

        // Assert
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task OnSelectedDeviceTypeChanged_SetsHasChanges()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        // Act
        _viewModel.SelectedDeviceType = DeviceType.EdenXp;

        // Assert
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task InitializeAsync_WithDeviceType_LoadsDeviceType()
    {
        // Arrange
        var boardType = new BoardType("TestType", 5);
        _boardService.SeedBoardTypes(boardType);
        var dict = Dictionary.Restore(1, "TestDict", DeviceType.OptimusXp, boardType, "Desc", []);
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Equal(DeviceType.OptimusXp, _viewModel.SelectedDeviceType);
    }

    [Fact]
    public void DeviceTypes_ExposesAllValues()
    {
        Assert.Equal(Enum.GetValues<DeviceType>().Length, _viewModel.DeviceTypes.Count);
    }

    [Fact]
    public async Task SaveCommand_WithBoardType_CreatesWithBoardType()
    {
        // Arrange
        var boardType = new BoardType("TestType", 5);
        _boardService.SeedBoardTypes(boardType);
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _viewModel.SelectedDeviceType = DeviceType.OptimusXp;
        _viewModel.SelectedBoardType = _viewModel.AvailableBoardTypes.First();

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("AddAsync:NewDict", _dictionaryService.MethodCalls);
    }

    [Fact]
    public async Task BoardTypeItem_HasCorrectProperties()
    {
        // Arrange
        var boardType = new BoardType("TestType", 42);
        _boardService.SeedBoardTypes(boardType);

        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        var item = _viewModel.AvailableBoardTypes.First();
        Assert.Equal("TestType", item.Name);
        Assert.Equal(42, item.FirmwareType);
        Assert.True(item.Id > 0);
    }
}
#endif
