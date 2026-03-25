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
        Assert.Equal("80", _viewModel.CodeHighHex); // IsResponse=true → 0x80
        Assert.Equal("34", _viewModel.CodeLowHex);
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
        // CodeHighHex è computed automaticamente da IsResponse (0x00 per comando)
        _viewModel.CodeLowHex = "01";

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
        // Arrange - IsResponse = true → CodeHighHex = 0x80
        _viewModel.IsResponse = true;
        _viewModel.CodeLowHex = "CD";

        // Assert
        Assert.Equal("0x80CD", _viewModel.FullCodeDisplay);
    }

    [Fact]
    public void CodeHighHex_DependsOnIsResponse()
    {
        // Default: IsResponse = false → CodeHighHex = 0x00
        Assert.Equal("00", _viewModel.CodeHighHex);

        // IsResponse = true → CodeHighHex = 0x80
        _viewModel.IsResponse = true;
        Assert.Equal("80", _viewModel.CodeHighHex);

        // Torna a false → CodeHighHex = 0x00
        _viewModel.IsResponse = false;
        Assert.Equal("00", _viewModel.CodeHighHex);
    }

    [Fact]
    public void FullCodeDisplay_Command_FormatsWithHigh00()
    {
        // Arrange - IsResponse = false → CodeHighHex = 0x00
        _viewModel.IsResponse = false;
        _viewModel.CodeLowHex = "1A";

        // Assert
        Assert.Equal("0x001A", _viewModel.FullCodeDisplay);
    }

    [Fact]
    public void FullCodeDisplay_Response_FormatsWithHigh80()
    {
        // Arrange - IsResponse = true → CodeHighHex = 0x80
        _viewModel.IsResponse = true;
        _viewModel.CodeLowHex = "1A";

        // Assert
        Assert.Equal("0x801A", _viewModel.FullCodeDisplay);
    }

    [Fact]
    public async Task SaveAsync_Command_UsesCodeHigh00()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestCmd";
        _viewModel.IsResponse = false;
        _viewModel.CodeLowHex = "05";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("00", _viewModel.CodeHighHex);
        Assert.Contains(_commandService.MethodCalls, m => m.StartsWith("AddAsync:TestCmd"));
    }

    [Fact]
    public async Task SaveAsync_Response_UsesCodeHigh80()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestResp";
        _viewModel.IsResponse = true;
        _viewModel.CodeLowHex = "05";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("80", _viewModel.CodeHighHex);
        Assert.Contains(_commandService.MethodCalls, m => m.StartsWith("AddAsync:TestResp"));
    }

    [Fact]
    public async Task LoadFromCommand_IsResponseTrue_SetsCodeHighTo80()
    {
        // Arrange - comando con IsResponse=true
        var command = new Command("ResponseCmd", 0x80, 0x10, true);
        _commandService.SeedData(command);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.True(_viewModel.IsResponse);
        Assert.Equal("80", _viewModel.CodeHighHex);
        Assert.Equal("10", _viewModel.CodeLowHex);
    }

    [Fact]
    public async Task LoadFromCommand_IsResponseFalse_SetsCodeHighTo00()
    {
        // Arrange - comando con IsResponse=false
        var command = new Command("NormalCmd", 0x00, 0x10, false);
        _commandService.SeedData(command);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.False(_viewModel.IsResponse);
        Assert.Equal("00", _viewModel.CodeHighHex);
        Assert.Equal("10", _viewModel.CodeLowHex);
    }

    // === Test DeleteCommand (nuovo dal refactoring) ===

    [Fact]
    public async Task DeleteCommand_WithConfirmation_DeletesAndGoesBack()
    {
        // Arrange
        var command = new Command("ToDelete", 0x10, 0x01, false);
        _commandService.SeedData(command);
        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.Yes;

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("DeleteAsync:1", _commandService.MethodCalls);
        Assert.True(_navigationService.GoBackCalled);
        Assert.Contains(_messageService.Messages, m => m.Message.Contains("eliminato"));
    }

    [Fact]
    public async Task DeleteCommand_WithCancel_DoesNotDelete()
    {
        // Arrange
        var command = new Command("ToKeep", 0x10, 0x01, false);
        _commandService.SeedData(command);
        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.No;

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert
        Assert.DoesNotContain(_commandService.MethodCalls, m => m.StartsWith("DeleteAsync"));
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteCommand_WhenNew_DoesNothing()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert
        Assert.DoesNotContain(_commandService.MethodCalls, m => m.StartsWith("DeleteAsync"));
        Assert.DoesNotContain(_dialogService.Calls, c => c.Type == "Confirm");
    }

    [Fact]
    public async Task DeleteCommand_WhenServiceThrows_ShowsErrorDialog()
    {
        // Arrange
        var command = new Command("ToDelete", 0x10, 0x01, false);
        _commandService.SeedData(command);
        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.Yes;
        _commandService.ExceptionToThrow = new Exception("Delete failed");

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.False(_navigationService.GoBackCalled);
    }
}
#endif
