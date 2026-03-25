#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DictionaryEditViewModel (Domain v2).
/// IsStandard flag, nessun DeviceType/BoardType.
/// </summary>
public class DictionaryEditViewModelTests
{
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockVariableService _variableService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly DictionaryEditViewModel _viewModel;

    public DictionaryEditViewModelTests()
    {
        _dictionaryService = new MockDictionaryService();
        _variableService = new MockVariableService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new DictionaryEditViewModel(
            _dictionaryService,
            _variableService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        await _viewModel.InitializeAsync(null);

        Assert.True(_viewModel.IsNew);
        Assert.Equal("Nuovo Dizionario", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        var dict = new Dictionary("Existing", "Desc");
        _dictionaryService.SeedData(dict);

        await _viewModel.InitializeAsync(1);

        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Dizionario", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_LoadsExistingData()
    {
        var dict = Dictionary.Restore(1, "TestDict", "TestDesc", true, []);
        _dictionaryService.SeedData(dict);

        await _viewModel.InitializeAsync(1);

        Assert.Equal("TestDict", _viewModel.Name);
        Assert.Equal("TestDesc", _viewModel.Description);
        Assert.True(_viewModel.IsStandard);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
        await _viewModel.InitializeAsync(999);

        Assert.Contains(_dialogService.Calls, c => c.Type == "Error" && c.Message.Contains("non trovato"));
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        await _viewModel.InitializeAsync(null);
        _dictionaryService.MethodCalls.Clear();
        await _viewModel.InitializeAsync(null);

        Assert.Empty(_dictionaryService.MethodCalls);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessage()
    {
        _dictionaryService.ExceptionToThrow = new Exception("Service error");

        await _viewModel.InitializeAsync(1);

        Assert.Equal("Service error", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task SaveCommand_CannotExecute_WhenNameEmpty()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = string.Empty;

        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_CanExecute_WhenNameSet()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Test";

        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _viewModel.Description = "NewDesc";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("AddAsync:"));
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_CallsUpdateAsync()
    {
        var dict = new Dictionary("Existing");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);
        _viewModel.Name = "Updated";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("UpdateAsync:"));
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_messageService.Messages, m =>
            m.Message.Contains("creato") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _dictionaryService.ExceptionToThrow = new Exception("Save failed");

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c => c.Type == "Error");
    }

    [Fact]
    public async Task OnNameChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        _viewModel.Name = "Changed";

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task OnDescriptionChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        _viewModel.Description = "Changed description";

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task OnIsStandardChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);
        Assert.False(_viewModel.HasChanges);

        _viewModel.IsStandard = true;

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveCommand_WithIsStandard_CreatesStandardDictionary()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Standard";
        _viewModel.IsStandard = true;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("AddAsync:Standard", _dictionaryService.MethodCalls);
    }

    // === Test nuove funzionalità refactoring ===

    [Fact]
    public async Task SaveCommand_WhenNew_StaysOnPage()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        // P1: Save non fa GoBack, resta sulla pagina
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_StaysOnPage()
    {
        var dict = new Dictionary("Existing");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);
        _viewModel.Name = "Updated";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_WhenNew_SetsEditingId_AndUpdatesIsNew()
    {
        await _viewModel.InitializeAsync(null);
        Assert.True(_viewModel.IsNew);

        _viewModel.Name = "NewDict";
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Dopo il primo salvataggio, IsNew diventa false
        Assert.False(_viewModel.IsNew);
    }

    [Fact]
    public async Task InitializeAsync_WithId_LoadsVariables()
    {
        var dict = new Dictionary("WithVars");
        _dictionaryService.SeedData(dict);

        await _viewModel.InitializeAsync(1);

        Assert.Contains($"GetByDictionaryIdAsync:1", _variableService.MethodCalls);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_DoesNotLoadVariables()
    {
        await _viewModel.InitializeAsync(null);

        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("GetByDictionaryIdAsync"));
    }

    [Fact]
    public async Task ReloadVariablesAsync_ReloadsOnlyVariables()
    {
        var dict = new Dictionary("Test");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);
        _variableService.MethodCalls.Clear();
        _dictionaryService.MethodCalls.Clear();

        await _viewModel.ReloadVariablesAsync();

        Assert.Contains($"GetByDictionaryIdAsync:1", _variableService.MethodCalls);
        // Non ricarica il dizionario
        Assert.DoesNotContain(_dictionaryService.MethodCalls, m => m.StartsWith("GetByIdAsync"));
    }

    [Fact]
    public async Task ReloadVariablesAsync_WhenNew_DoesNothing()
    {
        await _viewModel.InitializeAsync(null);
        _variableService.MethodCalls.Clear();

        await _viewModel.ReloadVariablesAsync();

        Assert.Empty(_variableService.MethodCalls);
    }

    [Fact]
    public async Task DeleteDictionaryCommand_ShowsAdminMessage()
    {
        await _viewModel.InitializeAsync(null);

        await _viewModel.DeleteDictionaryCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c =>
            c.Type == "Error" && c.Message.Contains("riservata"));
    }

    [Fact]
    public async Task AddVariableCommand_NavigatesToVariableEdit()
    {
        var dict = new Dictionary("Test");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);

        _viewModel.AddVariableCommand.Execute(null);

        Assert.Equal(ViewType.VariableEdit, _navigationService.LastNavigatedView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
        Assert.Equal(1, _navigationService.LastParameter?.ParentId);
    }

    [Fact]
    public async Task AddVariableCommand_WhenNew_DoesNotNavigate()
    {
        await _viewModel.InitializeAsync(null);

        _viewModel.AddVariableCommand.Execute(null);

        Assert.Null(_navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task EditVariableCommand_NavigatesToVariableEdit_WithId()
    {
        var dict = new Dictionary("Test");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);

        var item = new VariableListItem { Id = 42, Name = "TestVar" };
        _viewModel.EditVariableCommand.Execute(item);

        Assert.Equal(ViewType.VariableEdit, _navigationService.LastNavigatedView);
        Assert.Equal(42, _navigationService.LastParameter?.EntityId);
        Assert.Equal(1, _navigationService.LastParameter?.ParentId);
    }

    [Fact]
    public async Task EditVariableCommand_WithNull_DoesNotNavigate()
    {
        var dict = new Dictionary("Test");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);

        _viewModel.EditVariableCommand.Execute(null);

        Assert.Null(_navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task VariableSearchText_FiltersVariableList()
    {
        var dict = new Dictionary("Test");
        _dictionaryService.SeedData(dict);

        var v1 = new Variable("Temperature", 0x80, 0x01, Core.Enums.DataTypeKind.UInt16, Core.Enums.AccessMode.ReadOnly, "UInt16");
        var v2 = new Variable("Voltage", 0x80, 0x02, Core.Enums.DataTypeKind.UInt16, Core.Enums.AccessMode.ReadOnly, "UInt16");
        _variableService.SeedData(v1, v2);

        await _viewModel.InitializeAsync(1);
        Assert.Equal(2, _viewModel.Variables.Count);

        _viewModel.VariableSearchText = "Temp";

        Assert.Single(_viewModel.Variables);
        Assert.Equal("Temperature", _viewModel.Variables[0].Name);
    }

    [Fact]
    public async Task VariableSearchText_EmptyString_ShowsAll()
    {
        var dict = new Dictionary("Test");
        _dictionaryService.SeedData(dict);

        var v1 = new Variable("Temperature", 0x80, 0x01, Core.Enums.DataTypeKind.UInt16, Core.Enums.AccessMode.ReadOnly, "UInt16");
        var v2 = new Variable("Voltage", 0x80, 0x02, Core.Enums.DataTypeKind.UInt16, Core.Enums.AccessMode.ReadOnly, "UInt16");
        _variableService.SeedData(v1, v2);

        await _viewModel.InitializeAsync(1);
        _viewModel.VariableSearchText = "Temp";
        Assert.Single(_viewModel.Variables);

        _viewModel.VariableSearchText = "";

        Assert.Equal(2, _viewModel.Variables.Count);
    }

    // === Test CanSetStandard ===

    [Fact]
    public async Task CanSetStandard_TrueWhenNoStandardExists()
    {
        // Nessun dizionario standard nel mock
        await _viewModel.InitializeAsync(null);

        Assert.True(_viewModel.CanSetStandard);
    }

    [Fact]
    public async Task CanSetStandard_TrueWhenEditingTheStandardDictionary()
    {
        var dict = Dictionary.Restore(1, "Standard", "Desc", true, []);
        _dictionaryService.SeedData(dict);

        await _viewModel.InitializeAsync(1);

        Assert.True(_viewModel.IsStandard);
        Assert.True(_viewModel.CanSetStandard);
    }

    [Fact]
    public async Task CanSetStandard_FalseWhenAnotherStandardExists()
    {
        // Esiste già un dizionario standard (Id=1)
        var standard = Dictionary.Restore(1, "Standard", null, true, []);
        _dictionaryService.SeedData(standard);

        // Creo un nuovo dizionario (non standard)
        await _viewModel.InitializeAsync(null);

        Assert.False(_viewModel.CanSetStandard);
    }

    [Fact]
    public async Task CanSetStandard_FalseWhenEditingNonStandardAndStandardExists()
    {
        // Esiste il dizionario standard (Id=1) e un altro (Id=2)
        var standard = Dictionary.Restore(1, "Standard", null, true, []);
        var other = Dictionary.Restore(2, "Other", null, false, []);
        _dictionaryService.SeedData(standard, other);

        await _viewModel.InitializeAsync(2);

        Assert.False(_viewModel.IsStandard);
        Assert.False(_viewModel.CanSetStandard);
    }
}
#endif
