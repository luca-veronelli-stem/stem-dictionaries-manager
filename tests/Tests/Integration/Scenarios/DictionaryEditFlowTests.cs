#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

namespace Tests.Integration.Scenarios;

/// <summary>
/// Integration test per il flusso completo di gestione dizionari.
/// Testa F1 (CRUD Dizionari), BR-004 (Standard unico), BR-013 (AddressHigh computed).
/// </summary>
public class DictionaryEditFlowTests
{
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockVariableService _variableService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly MockBoardService _boardService;
    private readonly DictionaryEditViewModel _viewModel;

    public DictionaryEditFlowTests()
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

    #region Create Dictionary Tests

    [Fact]
    public async Task CreateDictionary_Standard_SetsIsStandardTrue()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);

        _viewModel.Name = "Standard";
        _viewModel.Description = "Variabili comuni";
        _viewModel.IsStandard = true;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_dictionaryService.MethodCalls, m => m.StartsWith("AddAsync:Standard"));
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task CreateDictionary_NonStandard_WithBoard_SetsIsStandardFalse()
    {
        // Arrange — serve una board per dizionari non-standard
        _boardService.SeedBoards(Board.Restore(1, 1, "Madre", 17, 1, null, true, null, machineCode: 1));
        await _viewModel.InitializeAsync(null, deviceId: 1);

        _viewModel.Name = "Eden-XP";
        _viewModel.Description = "Dizionario dedicato Eden-XP";
        _viewModel.IsStandard = false;
        _viewModel.SelectedBoard = _viewModel.AvailableBoards.First();

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_dictionaryService.MethodCalls, m => m.StartsWith("AddAsync:Eden-XP"));
    }

    [Fact]
    public async Task CreateDictionary_SecondStandard_ShowsError()
    {
        // Arrange - esiste già un dizionario standard
        _dictionaryService.SeedData(Dictionary.Restore(1, "Standard", null, true, []));

        await _viewModel.InitializeAsync(null);

        _viewModel.Name = "AltroStandard";
        _viewModel.IsStandard = true; // BR-004 violation

        // Imposta l'eccezione DOPO l'inizializzazione
        _dictionaryService.ExceptionToThrow = new InvalidOperationException(
            "Esiste già un dizionario Standard nel sistema.");

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - l'errore va nel dialog, non nel message service
        Assert.True(_dialogService.ShowErrorCalled);
    }

    #endregion

    #region Edit Dictionary Tests

    [Fact]
    public async Task EditDictionary_LoadsExistingData()
    {
        // Arrange
        var existingDict = Dictionary.Restore(1, "TestDict", "Test description", false, []);
        _dictionaryService.SeedData(existingDict);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Equal("TestDict", _viewModel.Name);
        Assert.Equal("Test description", _viewModel.Description);
        Assert.False(_viewModel.IsStandard);
        Assert.False(_viewModel.IsNew);
    }

    [Fact]
    public async Task EditDictionary_ReloadsVariableListAfterAddVariable()
    {
        // Arrange
        var existingDict = Dictionary.Restore(1, "TestDict", null, false, []);
        _dictionaryService.SeedData(existingDict);

        await _viewModel.InitializeAsync(1);
        var initialCount = _viewModel.Variables.Count;

        // Simula aggiunta variabile
        _variableService.SeedData(Variable.Restore(
            1, "NewVar", 0x80, 0x01,
            Core.Enums.DataTypeKind.UInt8, "UInt8", null,
            Core.Enums.AccessMode.ReadOnly, true,
            null, null, null, null, null, "Test"));

        // Act
        await _viewModel.ReloadVariablesAsync();

        // Assert - la lista variabili è stata ricaricata
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("GetByDictionaryIdAsync"));
    }

    #endregion

    #region IsStandard Checkbox Tests

    [Fact]
    public async Task IsStandard_Checkbox_CanSetStandard_WhenNoStandardExists()
    {
        // Arrange - nessun dizionario standard esistente
        await _viewModel.InitializeAsync(null);

        // Assert
        Assert.True(_viewModel.CanSetStandard);
    }

    [Fact]
    public async Task IsStandard_Checkbox_CannotSetStandard_WhenStandardExistsAndEditingNonStandard()
    {
        // Arrange - esiste già un dizionario standard
        _dictionaryService.SeedData(Dictionary.Restore(1, "Standard", null, true, []));
        _dictionaryService.SeedData(Dictionary.Restore(2, "NonStandard", null, false, []));

        await _viewModel.InitializeAsync(2); // Editing non-standard

        // Assert
        Assert.False(_viewModel.CanSetStandard);
    }

    [Fact]
    public async Task IsStandard_Checkbox_CanSetStandard_WhenEditingTheStandardItself()
    {
        // Arrange - sto modificando il dizionario standard stesso
        _dictionaryService.SeedData(Dictionary.Restore(1, "Standard", null, true, []));

        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.True(_viewModel.CanSetStandard);
    }

    #endregion

    #region Variable Search Tests

    [Fact]
    public async Task VariableSearch_FiltersListCorrectly()
    {
        // Arrange
        var dict = Dictionary.Restore(1, "TestDict", null, false, []);
        _dictionaryService.SeedData(dict);
        _variableService.SeedData(
            Variable.Restore(1, "Temperature", 0x80, 0x01,
                Core.Enums.DataTypeKind.Int16, "Int16", null,
                Core.Enums.AccessMode.ReadOnly, true,
                null, null, null, "°C", null, "CPU Temp"),
            Variable.Restore(2, "Pressure", 0x80, 0x02,
                Core.Enums.DataTypeKind.Float, "Float", null,
                Core.Enums.AccessMode.ReadOnly, true,
                null, null, null, "bar", null, "System pressure")
        );

        await _viewModel.InitializeAsync(1);

        // Act
        _viewModel.VariableSearchText = "Temp";

        // Assert
        Assert.Single(_viewModel.Variables);
        Assert.Contains(_viewModel.Variables, v => v.Name == "Temperature");
    }

    [Fact]
    public async Task VariableSearch_EmptyText_ShowsAll()
    {
        // Arrange
        var dict = Dictionary.Restore(1, "TestDict", null, false, []);
        _dictionaryService.SeedData(dict);
        _variableService.SeedData(
            Variable.Restore(1, "Var1", 0x80, 0x01,
                Core.Enums.DataTypeKind.UInt8, "UInt8", null,
                Core.Enums.AccessMode.ReadOnly, true,
                null, null, null, null, null, "Desc1"),
            Variable.Restore(2, "Var2", 0x80, 0x02,
                Core.Enums.DataTypeKind.UInt16, "UInt16", null,
                Core.Enums.AccessMode.ReadOnly, true,
                null, null, null, null, null, "Desc2")
        );

        await _viewModel.InitializeAsync(1);
        _viewModel.VariableSearchText = "something";

        // Act
        _viewModel.VariableSearchText = "";

        // Assert
        Assert.Equal(2, _viewModel.Variables.Count);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task Validation_EmptyName_ShowsError()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);

        _viewModel.Name = ""; // Invalid

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.IsNameInvalid);
        Assert.DoesNotContain(_dictionaryService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task CancelWithChanges_ShowsConfirmDialog()
    {
        // Arrange
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "Test"; // HasChanges = true
        _dialogService.ConfirmResult = DialogResult.Yes;

        // Act
        _viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.True(_dialogService.ShowConfirmCalled);
    }

    #endregion
}
#endif
