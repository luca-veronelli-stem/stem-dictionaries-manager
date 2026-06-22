#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
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
            _messageService,
            NullLogger<CommandEditViewModel>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        // Act
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.True(_viewModel.IsNew);
        Assert.Equal("New Command", _viewModel.FormTitle);
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
        Assert.Equal("Edit Command", _viewModel.FormTitle);
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
        Assert.Equal(2, _viewModel.ParameterItems.Count);
        Assert.Equal("param1", _viewModel.ParameterItems[0].Description);
        Assert.Equal("param2", _viewModel.ParameterItems[1].Description);
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
    public async Task SaveCommand_Validates_WhenNameEmpty()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";
        _viewModel.CodeLowHex = "01";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsNameInvalid);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_commandService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task SaveCommand_Validates_WhenCodeLowEmpty()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestCmd";
        _viewModel.CodeLowHex = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsCodeLowInvalid);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_commandService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task SaveCommand_WithNoParameters_Succeeds()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestCmd";
        _viewModel.CodeLowHex = "01";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_commandService.MethodCalls, m => m.StartsWith("AddAsync:TestCmd"));
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
    public async Task SaveCommand_SerializesParameterItems()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "TestCommand";
        _viewModel.CodeLowHex = "01";
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.ParameterItems[0].SizeBytes = "2";
        _viewModel.ParameterItems[0].Description = "Indirizzo";
        _viewModel.ParameterItems[1].SizeBytes = "1";
        _viewModel.ParameterItems[1].Description = "Modalità";

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
        _viewModel.CodeLowHex = "01";

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
        _viewModel.CodeLowHex = "01";
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
        Assert.Contains(_messageService.Messages, m => m.Message.Contains("deleted"));
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

    // === Test CancelCommand con HasChanges ===

    [Fact]
    public async Task CancelCommand_WithChanges_ShowsConfirmDialog()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c =>
            c.Type == "Confirm" && c.Message.Contains("discard"));
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

    // === Test ParameterItems (P1-P5 dalla specifica Lean 4) ===

    [Fact]
    public void HasParameters_FalseWhenEmpty()
    {
        Assert.False(_viewModel.HasParameters);
    }

    [Fact]
    public void HasParameters_TrueAfterAddParameter()
    {
        _viewModel.AddParameterCommand.Execute(null);
        Assert.True(_viewModel.HasParameters);
    }

    [Fact]
    public void HasParameters_FalseAfterRemoveLastParameter()
    {
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.RemoveLastParameterCommand.Execute(null);
        Assert.False(_viewModel.HasParameters);
    }

    [Fact]
    public void AddParameter_GeneratesCorrectNumberOfItems()
    {
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        Assert.Equal(3, _viewModel.ParameterItems.Count);
    }

    [Fact]
    public void AddParameter_PreservesExistingData()
    {
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.ParameterItems[0].SizeBytes = "2";
        _viewModel.ParameterItems[0].Description = "Indirizzo";
        _viewModel.ParameterItems[1].SizeBytes = "1";
        _viewModel.ParameterItems[1].Description = "Modalità";

        // Aggiungi un terzo: i primi 2 devono restare intatti
        _viewModel.AddParameterCommand.Execute(null);
        Assert.Equal(3, _viewModel.ParameterItems.Count);
        Assert.Equal("2", _viewModel.ParameterItems[0].SizeBytes);
        Assert.Equal("Indirizzo", _viewModel.ParameterItems[0].Description);
        Assert.Equal("1", _viewModel.ParameterItems[1].SizeBytes);
        Assert.Equal("Modalità", _viewModel.ParameterItems[1].Description);
    }

    [Fact]
    public void RemoveLastParameter_RemovesLastItem()
    {
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.ParameterItems[2].Description = "Third";

        _viewModel.RemoveLastParameterCommand.Execute(null);
        Assert.Equal(2, _viewModel.ParameterItems.Count);
    }

    [Fact]
    public void RemoveLastParameter_WhenEmpty_DoesNothing()
    {
        _viewModel.RemoveLastParameterCommand.Execute(null);
        Assert.Empty(_viewModel.ParameterItems);
    }

    [Fact]
    public void ParameterItems_IndexDisplayFormatsCorrectly()
    {
        // P5: IndexDisplay mostra solo il numero
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        Assert.Equal("1", _viewModel.ParameterItems[0].IndexDisplay);
        Assert.Equal("2", _viewModel.ParameterItems[1].IndexDisplay);
    }

    [Fact]
    public async Task InitializeAsync_LoadsStructuredParameters()
    {
        // Formato strutturato "size|description"
        var command = new Command("WriteReg", 0x00, 0x10, false, ["2|Indirizzo", "4|Valore"]);
        _commandService.SeedData(command);

        await _viewModel.InitializeAsync(1);

        Assert.Equal(2, _viewModel.ParameterItems.Count);
        Assert.Equal("2", _viewModel.ParameterItems[0].SizeBytes);
        Assert.Equal("Indirizzo", _viewModel.ParameterItems[0].Description);
        Assert.Equal("4", _viewModel.ParameterItems[1].SizeBytes);
        Assert.Equal("Valore", _viewModel.ParameterItems[1].Description);
    }

    [Fact]
    public async Task InitializeAsync_LoadsLegacyParameters()
    {
        // P2: Legacy fallback — stringhe senza '|'
        var command = new Command("OldCmd", 0x00, 0x20, false, ["param1", "param2"]);
        _commandService.SeedData(command);

        await _viewModel.InitializeAsync(1);

        Assert.Equal(2, _viewModel.ParameterItems.Count);
        Assert.Equal("", _viewModel.ParameterItems[0].SizeBytes);
        Assert.Equal("param1", _viewModel.ParameterItems[0].Description);
        Assert.Equal("", _viewModel.ParameterItems[1].SizeBytes);
        Assert.Equal("param2", _viewModel.ParameterItems[1].Description);
    }

    [Fact]
    public async Task SaveCommand_SerializesInPipeFormat()
    {
        // P1: Roundtrip serialize
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "WriteReg";
        _viewModel.CodeLowHex = "10";
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.ParameterItems[0].SizeBytes = "2";
        _viewModel.ParameterItems[0].Description = "Indirizzo";
        _viewModel.ParameterItems[1].SizeBytes = "4";
        _viewModel.ParameterItems[1].Description = "Valore";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var saved = _commandService.GetSavedCommand();
        Assert.NotNull(saved);
        Assert.Equal(2, saved.Parameters.Count);
        Assert.Equal("2|Indirizzo", saved.Parameters[0]);
        Assert.Equal("4|Valore", saved.Parameters[1]);
    }

    [Fact]
    public void AddParameter_SetsHasChanges()
    {
        _viewModel.HasChanges = false;
        _viewModel.AddParameterCommand.Execute(null);
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public void RemoveLastParameter_SetsHasChanges()
    {
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.HasChanges = false;

        _viewModel.RemoveLastParameterCommand.Execute(null);
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public void ParameterItem_PropertyChanged_SetsHasChanges()
    {
        _viewModel.AddParameterCommand.Execute(null);
        _viewModel.HasChanges = false;

        _viewModel.ParameterItems[0].Description = "Changed";
        Assert.True(_viewModel.HasChanges);
    }

    // === Validation feedback tests ===

    [Fact]
    public void ValidationProperties_FalseBeforeSaveAttempt()
    {
        _viewModel.Name = "";
        _viewModel.CodeLowHex = "";

        Assert.False(_viewModel.IsNameInvalid);
        Assert.False(_viewModel.IsCodeLowInvalid);
    }

    [Fact]
    public async Task SaveCommand_ValidationClearsAfterFixingFields()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";
        _viewModel.CodeLowHex = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsNameInvalid);
        Assert.True(_viewModel.IsCodeLowInvalid);

        _viewModel.Name = "Fixed";
        Assert.False(_viewModel.IsNameInvalid);
        _viewModel.CodeLowHex = "01";
        Assert.False(_viewModel.IsCodeLowInvalid);
    }

    [Fact]
    public async Task SaveCommand_ValidationMessage_ListsMissingFields()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";
        _viewModel.CodeLowHex = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var (Message, Severity) = _messageService.Messages.First(m => m.Severity == MessageSeverity.Warning);
        Assert.Contains("Name", Message);
        Assert.Contains("Code", Message);
    }

    [Fact]
    public async Task ValidationProperties_FalseAfterSave_WhenFieldsValid()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "ValidCmd";
        _viewModel.CodeLowHex = "01";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(_viewModel.IsNameInvalid);
        Assert.False(_viewModel.IsCodeLowInvalid);
    }

    [Fact]
    public async Task LoadFromCommand_NoParams_HasNoParameterItems()
    {
        var command = new Command("NoParams", 0x00, 0x10, false);
        _commandService.SeedData(command);

        await _viewModel.InitializeAsync(1);

        Assert.False(_viewModel.HasParameters);
        Assert.Empty(_viewModel.ParameterItems);
    }

    // === Test gap aggiuntivi ===

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_ShowsErrorAndSetsMessage()
    {
        _commandService.ExceptionToThrow = new Exception("DB connection failed");

        await _viewModel.InitializeAsync(1);

        Assert.Equal("DB connection failed", _viewModel.ErrorMessage);
        Assert.True(_dialogService.ShowErrorCalled);
    }

    [Fact]
    public void CodeLowHex_DefaultEmpty_FullCodeDisplayShows0x0000()
    {
        Assert.Equal(string.Empty, _viewModel.CodeLowHex);
        Assert.Equal("0x0000", _viewModel.FullCodeDisplay);
    }   

    [Fact]
    public async Task LoadFromCommand_WithParams_HasChangesRemainsFalse()
    {
        var command = new Command("Cmd", 0x00, 0x10, false, ["2|Addr", "1|Mode"]);
        _commandService.SeedData(command);

        await _viewModel.InitializeAsync(1);

        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveCommand_WithZeroParams_SavesEmptyList()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "EmptyCmd";
        _viewModel.CodeLowHex = "01";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var saved = _commandService.GetSavedCommand();
        Assert.NotNull(saved);
        Assert.Empty(saved.Parameters);
    }

    [Fact]
    public async Task Validate_CalledTwice_DoesNotDuplicateMessages()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";
        _viewModel.CodeLowHex = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);
        await _viewModel.SaveCommand.ExecuteAsync(null);

        var warnings = _messageService.Messages.Where(m => m.Severity == MessageSeverity.Warning).ToList();
        Assert.Equal(2, warnings.Count);
        Assert.Equal(warnings[0].Message, warnings[1].Message);
    }
}
#endif
