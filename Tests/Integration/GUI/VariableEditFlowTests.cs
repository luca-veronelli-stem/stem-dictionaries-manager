#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso completo di gestione variabili.
/// </summary>
public class VariableEditFlowTests
{
    private readonly MockVariableService _variableService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly VariableEditViewModel _viewModel;

    public VariableEditFlowTests()
    {
        _variableService = new MockVariableService();
        _dictionaryService = new MockDictionaryService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        // Seed dizionario non-standard (AddressHigh = 0x80)
        _dictionaryService.SeedData(new Dictionary("TestDict", null, isStandard: false));

        _viewModel = new VariableEditViewModel(
            _variableService,
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task CreateVariable_WithValidData_SavesAndNavigatesBack()
    {
        // Arrange - dizionario non-standard (AddressHigh = 0x80)
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "Temperature";
        // AddressHighHex è computed automaticamente (0x80 per non-standard)
        _viewModel.AddressLowHex = "10";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Int16;
        _viewModel.SelectedAccessMode = AccessMode.ReadOnly;
        _viewModel.MinValue = -40;
        _viewModel.MaxValue = 125;
        _viewModel.Unit = "°C";
        _viewModel.Description = "CPU Temperature";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync:1:Temperature"));
        Assert.True(_navigationService.GoBackCalled);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task CreateBitmappedVariable_AutoCreatesWord0_AndCanAddWords()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "StatusFlags";
        _viewModel.AddressLowHex = "20";
        _viewModel.Description = "Status flags";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;

        // Assert - Word 0 creata automaticamente
        Assert.Single(_viewModel.WordGroups);
        Assert.Equal("Bitmapped[1]", _viewModel.DataTypeForSave);

        // Act - Aggiungi una word
        _viewModel.AddWordCommand.Execute(null);
        Assert.Equal(2, _viewModel.WordGroups.Count);
        Assert.Equal("Bitmapped[2]", _viewModel.DataTypeForSave);

        // Act - Salva
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - salva con 2 word
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task CreateStringVariable_RequiresSize()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "DeviceName";
        _viewModel.AddressLowHex = "30";
        _viewModel.Description = "Device name";
        _viewModel.SelectedDataTypeKind = DataTypeKind.String;

        // Act - prova a salvare senza Size
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - validazione fallisce
        Assert.True(_viewModel.IsDataTypeParamInvalid);
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));

        // Act - Imposta Size e risalva
        _viewModel.DataTypeParam = 20;
        _messageService.Reset();
        _variableService.MethodCalls.Clear();
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - ora salva
        Assert.False(_viewModel.IsDataTypeParamInvalid);
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task EditExistingVariable_LoadsDataCorrectly()
    {
        // Arrange
        var existingVar = Variable.Restore(
            id: 1,
            name: "ExistingTemp",
            addressHigh: 0x80,
            addressLow: 0x10,
            dataTypeKind: DataTypeKind.Int16,
            dataTypeRaw: "Int16",
            dataTypeParam: null,
            accessMode: AccessMode.ReadWrite,
            isEnabled: true,
            format: "%.1f",
            minValue: -40,
            maxValue: 125,
            unit: "°C",
            usage: null,
            description: "Existing variable");
        _variableService.SeedData(existingVar);

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1);

        // Assert
        Assert.Equal("ExistingTemp", _viewModel.Name);
        Assert.Equal("80", _viewModel.AddressHighHex);
        Assert.Equal("10", _viewModel.AddressLowHex);
        Assert.Equal(DataTypeKind.Int16, _viewModel.SelectedDataTypeKind);
        Assert.Equal(AccessMode.ReadWrite, _viewModel.SelectedAccessMode);
        Assert.Equal(-40, _viewModel.MinValue);
        Assert.Equal(125, _viewModel.MaxValue);
        Assert.Equal("°C", _viewModel.Unit);
    }

    [Fact]
    public async Task EditVariable_WithOtherType_LoadsCustomType()
    {
        // Arrange
        var customVar = Variable.Restore(
            id: 1,
            name: "CustomData",
            addressHigh: 0x80,
            addressLow: 0x50,
            dataTypeKind: DataTypeKind.Other,
            dataTypeRaw: "MyCustomStruct",
            dataTypeParam: null,
            accessMode: AccessMode.ReadOnly,
            isEnabled: true,
            format: null,
            minValue: null,
            maxValue: null,
            unit: null,
            usage: null,
            description: null);
        _variableService.SeedData(customVar);

        // Act - SeedData assegna _nextId=1
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1);

        // Assert
        Assert.Equal(DataTypeKind.Other, _viewModel.SelectedDataTypeKind);
        Assert.Equal("MyCustomStruct", _viewModel.CustomDataType);
        Assert.True(_viewModel.IsDataTypeOther);
    }

    [Fact]
    public async Task MinMaxValidation_PreventsInvalidSave()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Test desc";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        _viewModel.MinValue = 100;
        _viewModel.MaxValue = 50; // Invalid: min > max

        // Assert
        Assert.False(_viewModel.IsMinMaxValid);

        // Act - prova a salvare
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - validazione fallisce
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));

        // Act - Fix validation
        _viewModel.MaxValue = 200;

        // Assert
        Assert.True(_viewModel.IsMinMaxValid);
    }

    [Fact]
    public async Task AddressDisplay_UpdatesCorrectly()
    {
        // Arrange - dizionario non-standard (AddressHigh = 0x80)
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        // Assert initial - AddressHigh è 0x80 (non-standard), AddressLow è vuoto (0x00)
        Assert.Equal("0x8000", _viewModel.FullAddressDisplay);

        // Act - cambia solo AddressLow (AddressHigh è computed)
        _viewModel.AddressLowHex = "10";
        Assert.Equal("0x8010", _viewModel.FullAddressDisplay);

        _viewModel.AddressLowHex = "FF";
        Assert.Equal("0x80FF", _viewModel.FullAddressDisplay);
    }

    [Fact]
    public async Task AddressHigh_DependsOnDictionaryType()
    {
        // Test con dizionario Standard (AddressHigh = 0x00)
        _dictionaryService.Reset();
        _dictionaryService.SeedData(new Dictionary("StandardDict", null, isStandard: true));

        var viewModelStandard = new VariableEditViewModel(
            _variableService,
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);

        await viewModelStandard.InitializeAsync(null, dictionaryId: 1);
        viewModelStandard.AddressLowHex = "10";

        // Assert - dizionario Standard → AddressHigh = 0x00
        Assert.Equal("00", viewModelStandard.AddressHighHex);
        Assert.Equal("0x0010", viewModelStandard.FullAddressDisplay);
    }

    [Fact]
    public async Task CancelWithChanges_ShowsConfirmDialog()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.Name = "ModifiedName";
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.No;

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_dialogService.ShowConfirmCalled);
        Assert.False(_navigationService.GoBackCalled); // Should not navigate back
    }

    [Fact]
    public async Task CancelWithChanges_ConfirmedYes_NavigatesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.Name = "ModifiedName";
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.Yes;

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveBitmappedVariable_WithBitInterpretations_CallsUpdateBitInterpretations()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "StatusFlags";
        // AddressHighHex è computed automaticamente (0x80 per non-standard)
        _viewModel.AddressLowHex = "40";
        _viewModel.Description = "Status flags bitmapped";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 1;

        // Modifica il meaning della riga iniziale
        _viewModel.WordGroups[0].Items[0].Meaning = "Motor Running";
        // Aggiungi un altro bit
        _viewModel.AddBitToWordCommand.Execute(_viewModel.WordGroups[0]);
        _viewModel.WordGroups[0].Items[1].Meaning = "Error Flag";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync:1:StatusFlags"));
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("UpdateBitInterpretationsAsync:"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task LoadBitmappedVariable_PopulatesWordGroups()
    {
        // Arrange - SeedData assegna _nextId=1
        var bitmappedVar = Variable.Restore(
            id: 1,
            name: "BitsVar",
            addressHigh: 0x80,
            addressLow: 0x50,
            dataTypeKind: DataTypeKind.Bitmapped,
            dataTypeRaw: "Bitmapped",
            dataTypeParam: 2,
            accessMode: AccessMode.ReadOnly,
            isEnabled: true,
            format: null,
            minValue: null,
            maxValue: null,
            unit: null,
            usage: null,
            description: null);
        _variableService.SeedData(bitmappedVar);

        // Seed bit interpretations nel mock (ID 1, coerente con SeedData)
        _variableService.SeedBitInterpretations(1,
        [
            new BitInterpretation(1, 0, 0, "Motor", null),
            new BitInterpretation(1, 0, 3, "Pump", null),
            new BitInterpretation(1, 1, 0, "Alarm", null)
        ]);

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1);

        // Assert
        Assert.Equal(2, _viewModel.WordGroups.Count);
        // Word 0: 2 items (BitIndex 0, 3)
        Assert.Equal(2, _viewModel.WordGroups[0].Items.Count);
        Assert.Equal("Motor", _viewModel.WordGroups[0].Items[0].Meaning);
        Assert.Equal("Pump", _viewModel.WordGroups[0].Items[1].Meaning);
        // Word 1: 1 item (BitIndex 0)
        Assert.Single(_viewModel.WordGroups[1].Items);
        Assert.Equal("Alarm", _viewModel.WordGroups[1].Items[0].Meaning);
    }
}
#endif
