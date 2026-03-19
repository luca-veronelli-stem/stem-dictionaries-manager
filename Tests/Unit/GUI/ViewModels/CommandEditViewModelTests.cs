#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per CommandEditViewModel.
/// </summary>
public class CommandEditViewModelTests
{
    private readonly MockCommandService _commandService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly CommandEditViewModel _viewModel;

    public CommandEditViewModelTests()
    {
        _commandService = new MockCommandService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new CommandEditViewModel(
            _commandService,
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
        Assert.Equal("Nuovo Comando", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        // Arrange
        var command = new Command("Existing", 0x10, 0x01, false);
        _commandService.SeedData(command);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Comando", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_LoadsExistingData()
    {
        // Arrange
        var command = new Command("ReadStatus", 0x12, 0x34, true, ["param1", "param2"]);
        _commandService.SeedData(command);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Equal("ReadStatus", _viewModel.Name);
        Assert.Equal(0x12, _viewModel.CodeHigh);
        Assert.Equal(0x34, _viewModel.CodeLow);
        Assert.True(_viewModel.IsResponse);
        Assert.Contains("param1", _viewModel.ParametersText);
        Assert.Contains("param2", _viewModel.ParametersText);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
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
        await _viewModel.InitializeAsync(null);
        _commandService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.Empty(_commandService.MethodCalls);
    }

    [Fact]
    public void SaveCommand_CannotExecute_WhenNameEmpty()
    {
        // Arrange
        _viewModel.Name = "";

        // Assert
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveCommand_CanExecute_WhenNameSet()
    {
        // Arrange
        _viewModel.Name = "TestCommand";

        // Assert
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewCommand";
        _viewModel.CodeHigh = 0x20;
        _viewModel.CodeLow = 0x01;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_commandService.MethodCalls, m => m.StartsWith("AddAsync:NewCommand"));
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_CallsUpdateAsync()
    {
        // Arrange
        var command = new Command("Existing", 0x10, 0x01, false);
        _commandService.SeedData(command);
        await _viewModel.InitializeAsync(1);
        _viewModel.Name = "UpdatedName";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("UpdateAsync:1", _commandService.MethodCalls);
    }

    [Fact]
    public async Task SaveCommand_ParsesParametersFromText()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestCommand";
        _viewModel.ParametersText = "param1\r\nparam2\r\nparam3";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_commandService.MethodCalls, m => m.StartsWith("AddAsync:TestCommand"));
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestCommand";

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
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestCommand";
        _commandService.ExceptionToThrow = new Exception("Save failed");

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
        await _viewModel.InitializeAsync(null);

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public void FullCodeDisplay_FormatsCorrectly()
    {
        // Arrange
        _viewModel.CodeHigh = 0xAB;
        _viewModel.CodeLow = 0xCD;

        // Assert
        Assert.Equal("0xABCD", _viewModel.FullCodeDisplay);
    }
}
#endif
