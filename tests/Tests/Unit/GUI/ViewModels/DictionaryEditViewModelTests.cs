#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Core.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Tests for DictionaryEditViewModel (Domain v2).
/// IsStandard flag, no deviceId/BoardType.
/// </summary>
public class DictionaryEditViewModelTests
{
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockVariableService _variableService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly MockBoardService _boardService;
    private readonly DictionaryEditViewModel _viewModel;

    public DictionaryEditViewModelTests()
    {
        _dictionaryService = new MockDictionaryService();
        _variableService = new MockVariableService();
        _boardService = new MockBoardService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new DictionaryEditViewModel(
            _dictionaryService,
            _variableService,
            _boardService,
            _navigationService,
            _dialogService,
            _messageService,
            NullLogger<DictionaryEditViewModel>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        await _viewModel.InitializeAsync(null);

        Assert.True(_viewModel.IsNew);
        Assert.Equal("New Dictionary", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        var dict = new Dictionary("Existing", "Desc");
        _dictionaryService.SeedData(dict);

        await _viewModel.InitializeAsync(1);

        Assert.False(_viewModel.IsNew);
        Assert.Equal("Edit Dictionary", _viewModel.FormTitle);
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

        Assert.Contains(_dialogService.Calls, c => c.Type == "Error" && c.Message.Contains("not found"));
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
    public async Task SaveCommand_Validates_WhenNameEmpty()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = string.Empty;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsNameInvalid);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_dictionaryService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task SaveCommand_WhenNameValid_Succeeds()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Test";
        _viewModel.IsStandard = true;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.False(_viewModel.IsNameInvalid);
        Assert.Contains(_dictionaryService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _viewModel.Description = "NewDesc";
        _viewModel.IsStandard = true;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_dictionaryService.MethodCalls, c => c.StartsWith("AddAsync:"));
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_CallsUpdateAsync()
    {
        var dict = new Dictionary("Existing", isStandard: true);
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
        _viewModel.IsStandard = true;
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_messageService.Messages, m =>
            m.Message.Contains("created") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "NewDict";
        _viewModel.IsStandard = true;
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
        _viewModel.IsStandard = true;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        // P1: Save non fa GoBack, resta sulla pagina
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_StaysOnPage()
    {
        var dict = new Dictionary("Existing", isStandard: true);
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
        _viewModel.IsStandard = true;
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
    public async Task ReloadVariablesAsync_ReloadsSpecificAndStandardVariables()
    {
        // Arrange: dizionario non-standard con standard dict
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);
        await _viewModel.InitializeAsync(2);
        _variableService.MethodCalls.Clear();
        _dictionaryService.MethodCalls.Clear();

        // Act
        await _viewModel.ReloadVariablesAsync();

        // Assert: ricarica sia specifiche che standard
        Assert.Contains("GetByDictionaryIdAsync:2", _variableService.MethodCalls);
        Assert.Contains("GetStandardDictionaryAsync", _dictionaryService.MethodCalls);
        // Non ricarica il dizionario corrente
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
    public async Task DeleteDictionaryCommand_ConfirmedYes_DeletesAndGoesBack()
    {
        var dict = new Dictionary("ToDelete", "Desc");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);

        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.DeleteDictionaryCommand.ExecuteAsync(null);

        Assert.Contains(_dictionaryService.MethodCalls, c => c == "DeleteAsync:1");
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteDictionaryCommand_ConfirmedNo_DoesNotDelete()
    {
        var dict = new Dictionary("ToKeep", "Desc");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);

        _dialogService.ConfirmResult = DialogResult.No;

        await _viewModel.DeleteDictionaryCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_dictionaryService.MethodCalls, c => c.StartsWith("DeleteAsync"));
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteDictionaryCommand_ServiceThrows_ShowsError()
    {
        var dict = new Dictionary("ToFail", "Desc");
        _dictionaryService.SeedData(dict);
        await _viewModel.InitializeAsync(1);

        _dialogService.ConfirmResult = DialogResult.Yes;
        _dictionaryService.ExceptionToThrow = new InvalidOperationException("FK constraint");

        await _viewModel.DeleteDictionaryCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c =>
            c.Type == "Error" && c.Message.Contains("FK constraint"));
    }

    [Fact]
    public async Task DeleteDictionaryCommand_WhenNew_DoesNothing()
    {
        await _viewModel.InitializeAsync(null);

        await _viewModel.DeleteDictionaryCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_dictionaryService.MethodCalls, c => c.StartsWith("DeleteAsync"));
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

        var v1 = new Variable("Temperature", 0x80, 0x01, global::Core.Enums.DataTypeKind.UInt16, global::Core.Enums.AccessMode.ReadOnly, "UInt16");
        var v2 = new Variable("Voltage", 0x80, 0x02, global::Core.Enums.DataTypeKind.UInt16, global::Core.Enums.AccessMode.ReadOnly, "UInt16");
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

        var v1 = new Variable("Temperature", 0x80, 0x01, global::Core.Enums.DataTypeKind.UInt16, global::Core.Enums.AccessMode.ReadOnly, "UInt16");
        var v2 = new Variable("Voltage", 0x80, 0x02, global::Core.Enums.DataTypeKind.UInt16, global::Core.Enums.AccessMode.ReadOnly, "UInt16");
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

    // === Test CancelCommand ===

    [Fact]
    public async Task CancelCommand_WithNoChanges_GoesBack()
    {
        await _viewModel.InitializeAsync(null);

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.True(_navigationService.GoBackCalled);
        Assert.DoesNotContain(_dialogService.Calls, c => c.Type == "Confirm");
    }

    [Fact]
    public async Task CancelCommand_WithChanges_ShowsConfirmDialog()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Modified";
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
        _viewModel.Name = "Modified";
        _dialogService.ConfirmResult = DialogResult.No;

        await _viewModel.CancelCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c => c.Type == "Confirm");
        Assert.False(_navigationService.GoBackCalled);
    }

    // === Validation feedback tests ===

    [Fact]
    public void IsNameInvalid_FalseBeforeSaveAttempt()
    {
        _viewModel.Name = "";
        Assert.False(_viewModel.IsNameInvalid);
    }

    [Fact]
    public async Task SaveCommand_ValidationClearsAfterFixingName()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsNameInvalid);

        _viewModel.Name = "Fixed";
        Assert.False(_viewModel.IsNameInvalid);
    }

    [Fact]
    public async Task SaveCommand_ValidationMessage_ListsMissingFields()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var (Message, Severity) = _messageService.Messages.First(m => m.Severity == MessageSeverity.Warning);
        Assert.Contains("Name", Message);
    }

    // === StandardVariableOverride Tests ===

    /// <summary>Helper per creare variabili standard minimali per i test.</summary>
    private static Variable MakeStdVar(int id, string name, byte addrLow, bool isEnabled = true,
        string? description = null) =>
        Variable.Restore(id, name, 0x00, addrLow, DataTypeKind.UInt16, "UInt16", null,
            global::Core.Enums.AccessMode.ReadOnly, isEnabled, null, null, null, null, null, description);

    [Fact]
    public async Task InitializeAsync_NonStandard_LoadsStandardVariablesWithOverrides()
    {
        // Arrange: standard dict con 2 variabili
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01), MakeStdVar(11, "Stato", 0x02)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);

        _variableService.SeedOverrides(
            StandardVariableOverride.Restore(1, 2, 10, false, "Override descrizione"));

        // Act
        await _viewModel.InitializeAsync(2);

        // Assert
        Assert.Equal(2, _viewModel.StandardVariables.Count);

        var allarmi = _viewModel.StandardVariables.First(s => s.Name == "Allarmi");
        Assert.False(allarmi.IsEnabled); // overridden to false
        Assert.Equal("Override descrizione", allarmi.Description);

        var stato = _viewModel.StandardVariables.First(s => s.Name == "Stato");
        Assert.True(stato.IsEnabled); // template default
    }

    [Fact]
    public async Task StandardVariable_IsGloballyDisabled_SetCorrectly()
    {
        // Arrange: variabile deprecata (IsEnabled=false nel template)
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Deprecata", 0x01, isEnabled: false)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);

        // Act
        await _viewModel.InitializeAsync(2);

        // Assert
        var item = _viewModel.StandardVariables[0];
        Assert.True(item.IsGloballyDisabled);
        Assert.False(item.IsEnabled);
    }

    [Fact]
    public async Task InitializeAsync_Standard_DoesNotLoadStandardVariables()
    {
        // Arrange: dizionario Standard → non mostra sezione override
        var standardDict = Dictionary.Restore(1, "Standard", null, true, []);
        _dictionaryService.SeedData(standardDict);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Empty(_viewModel.StandardVariables);
        Assert.False(_viewModel.ShowStandardSection);
    }

    [Fact]
    public async Task EditStandardVariableCommand_NavigatesToVariableEdit_WithDictionaryContext()
    {
        // Arrange
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);

        await _viewModel.InitializeAsync(2);

        // Act
        var item = _viewModel.StandardVariables[0];
        _viewModel.EditStandardVariableCommand.Execute(item);

        // Assert: naviga a VariableEdit con ParentId=standard(1), DeviceId=current dict(2) per override mode
        Assert.Equal(ViewType.VariableEdit, _navigationService.LastNavigatedView);
        Assert.Equal(10, _navigationService.LastParameter?.EntityId);
        Assert.Equal(1, _navigationService.LastParameter?.ParentId);
        Assert.Equal(2, _navigationService.LastParameter?.DeviceId);
    }

    [Fact]
    public async Task EditStandardVariableCommand_WithNull_DoesNotNavigate()
    {
        // Arrange
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);
        await _viewModel.InitializeAsync(2);

        // Act
        _viewModel.EditStandardVariableCommand.Execute(null);

        // Assert
        Assert.Empty(_navigationService.NavigationHistory);
    }

    [Fact]
    public async Task SaveCommand_NewNonStandard_LoadsStandardVariablesAfterCreate()
    {
        // Arrange: standard dict con variabili template
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01)]);
        _dictionaryService.SeedData(standardDict);

        // Nuovo dizionario non-standard con board
        _boardService.SeedBoards(
            Board.Restore(1, 5, "Madre", 5, 1, null, true, dictionaryId: null, machineCode: 5));

        await _viewModel.InitializeAsync(null, deviceId: 5);
        _viewModel.Name = "NuovoDizionario";
        _viewModel.SelectedBoard = _viewModel.AvailableBoards[0];

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert: dopo la creazione, le variabili standard sono caricate
        Assert.NotEmpty(_viewModel.StandardVariables);
        Assert.True(_viewModel.ShowStandardSection);
    }

    [Fact]
    public async Task SaveCommand_NoInlineOverrideSave_DoesNotCallSetOverride()
    {
        // Arrange: save di dizionario esistente non deve salvare override inline
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);
        _boardService.SeedBoards(
            Board.Restore(1, 5, "Madre", 5, 1, null, true, dictionaryId: 2, machineCode: 5));

        await _viewModel.InitializeAsync(2, deviceId: 5);
        _viewModel.Name = "Eden-XP";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert: nessun SetOverrideAsync — gli override si salvano dalla VariableEdit
        Assert.DoesNotContain(_variableService.MethodCalls,
            c => c.StartsWith("SetOverrideAsync"));
    }

    [Fact]
    public async Task ReloadVariablesAsync_RefreshesStandardOverrides()
    {
        // Arrange: dizionario non-standard, simula ritorno da VariableEdit con override modificato
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);
        await _viewModel.InitializeAsync(2);

        // Simula override aggiunto dalla VariableEdit
        _variableService.SeedOverrides(
            StandardVariableOverride.Restore(1, 2, 10, false, "Override dopo edit"));

        // Act: GoBack chiama ReloadVariablesAsync
        await _viewModel.ReloadVariablesAsync();

        // Assert: la lista standard riflette il nuovo override
        var item = _viewModel.StandardVariables.First(s => s.Name == "Allarmi");
        Assert.False(item.IsEnabled);
        Assert.Equal("Override dopo edit", item.Description);
    }

    [Fact]
    public async Task SaveCommand_NewStandard_DoesNotLoadStandardSection()
    {
        // Arrange: nessun standard dict preesistente
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Standard";
        _viewModel.IsStandard = true;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert: un dizionario standard non mostra la sezione standard
        Assert.Empty(_viewModel.StandardVariables);
        Assert.False(_viewModel.ShowStandardSection);
    }

    [Fact]
    public async Task ReloadVariablesAsync_StandardDict_DoesNotReloadStandardVars()
    {
        // Arrange: dizionario standard
        var standardDict = Dictionary.Restore(1, "Standard", null, true, []);
        _dictionaryService.SeedData(standardDict);
        await _viewModel.InitializeAsync(1);
        _dictionaryService.MethodCalls.Clear();

        // Act
        await _viewModel.ReloadVariablesAsync();

        // Assert: non chiama GetStandardDictionaryAsync
        Assert.DoesNotContain(_dictionaryService.MethodCalls, m => m == "GetStandardDictionaryAsync");
    }

    [Fact]
    public void IsSpecificExpanded_DefaultsToTrue()
    {
        Assert.True(_viewModel.IsSpecificExpanded);
    }

    // === Test filtro "Mostra solo abilitate" ===

    [Fact]
    public void ShowOnlyEnabled_DefaultsToFalse()
    {
        Assert.False(_viewModel.ShowOnlyEnabled);
    }

    [Fact]
    public async Task ShowOnlyEnabled_FiltersStandardVariables()
    {
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01, isEnabled: true),
             MakeStdVar(11, "Deprecata", 0x02, isEnabled: false)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);

        await _viewModel.InitializeAsync(2);
        Assert.Equal(2, _viewModel.StandardVariables.Count);

        _viewModel.ShowOnlyEnabled = true;

        Assert.Single(_viewModel.StandardVariables);
        Assert.Equal("Allarmi", _viewModel.StandardVariables[0].Name);
    }

    [Fact]
    public async Task ShowOnlyEnabled_FiltersStandardOverriddenDisabled()
    {
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01, isEnabled: true),
             MakeStdVar(11, "Stato", 0x02, isEnabled: true)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);

        // Override disabilita "Stato" per questo dizionario
        _variableService.SeedOverrides(
            StandardVariableOverride.Restore(1, 2, 11, false, null));

        await _viewModel.InitializeAsync(2);
        Assert.Equal(2, _viewModel.StandardVariables.Count);

        _viewModel.ShowOnlyEnabled = true;

        Assert.Single(_viewModel.StandardVariables);
        Assert.Equal("Allarmi", _viewModel.StandardVariables[0].Name);
    }

    [Fact]
    public async Task ShowOnlyEnabled_Unchecked_ShowsAllStandardVariables()
    {
        var standardDict = Dictionary.Restore(1, "Standard", null, true,
            [MakeStdVar(10, "Allarmi", 0x01, isEnabled: true),
             MakeStdVar(11, "Deprecata", 0x02, isEnabled: false)]);
        var nonStdDict = Dictionary.Restore(2, "Eden-XP", null, false, []);
        _dictionaryService.SeedData(standardDict, nonStdDict);

        await _viewModel.InitializeAsync(2);
        _viewModel.ShowOnlyEnabled = true;
        Assert.Single(_viewModel.StandardVariables);

        _viewModel.ShowOnlyEnabled = false;

        Assert.Equal(2, _viewModel.StandardVariables.Count);
    }
}
#endif
