#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso BitInterpretation (F2.5).
/// Testa gestione variabili Bitmapped con WordGroups.
/// </summary>
public class BitInterpretationFlowTests
{
    private readonly MockVariableService _variableService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly VariableEditViewModel _viewModel;

    public BitInterpretationFlowTests()
    {
        _variableService = new MockVariableService();
        _dictionaryService = new MockDictionaryService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        // Seed dizionario non-standard
        _dictionaryService.SeedData(new Dictionary("TestDict", null, isStandard: false));

        _viewModel = new VariableEditViewModel(
            _variableService,
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task CreateBitmappedVariable_AutoCreatesWord0()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        // Act
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;

        // Assert
        Assert.Single(_viewModel.WordGroups);
        Assert.Equal(0, _viewModel.WordGroups[0].WordIndex);
    }

    [Fact]
    public async Task AddWord_CreatesNewWordGroup()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        Assert.Single(_viewModel.WordGroups);

        // Act
        _viewModel.AddWordCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.WordGroups.Count);
        Assert.Equal(1, _viewModel.WordGroups[1].WordIndex);
    }

    [Fact(Skip = "RemoveWordCommand behavior TBD - verifica manualmente")]
    public async Task RemoveWord_RemovesLastWordGroup()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.AddWordCommand.Execute(null); // Aggiungi una terza word
        Assert.Equal(3, _viewModel.WordGroups.Count);

        // Act
        _viewModel.RemoveWordCommand.Execute(null);

        // Assert
        Assert.Equal(2, _viewModel.WordGroups.Count);
    }

    [Fact]
    public async Task RemoveWord_CannotRemoveLastWord()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        Assert.Single(_viewModel.WordGroups);

        // Act - tenta di rimuovere l'unica word
        var initialCount = _viewModel.WordGroups.Count;
        _viewModel.RemoveWordCommand.Execute(null);

        // Assert - non deve rimuovere l'ultima word
        Assert.Equal(initialCount, _viewModel.WordGroups.Count);
    }

    [Fact]
    public async Task AddBit_ToWord_UpdatesBitInterpretations()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        var word0 = _viewModel.WordGroups[0];
        var initialCount = word0.Items.Count;

        // Act
        word0.TryAddBit();

        // Assert
        Assert.Equal(initialCount + 1, word0.Items.Count);
    }

    [Fact]
    public async Task RemoveBit_FromWord_UpdatesBitInterpretations()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        var word0 = _viewModel.WordGroups[0];
        word0.TryAddBit();
        word0.TryAddBit();
        var countBefore = word0.Items.Count;

        // Act - rimuovi l'ultimo bit
        var lastBit = word0.Items.LastOrDefault();
        if (lastBit is not null)
        {
            word0.TryRemoveBit(lastBit);
        }

        // Assert
        Assert.Equal(countBefore - 1, word0.Items.Count);
    }

    [Fact]
    public async Task SaveBitmapped_WithMultipleWords_SavesAllInterpretations()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.Name = "StatusFlags";
        _viewModel.AddressLowHex = "10";
        _viewModel.Description = "Status flags";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;

        // Aggiungi una word
        _viewModel.AddWordCommand.Execute(null);

        // Aggiungi bit alle word
        _viewModel.WordGroups[0].TryAddBit();
        _viewModel.WordGroups[0].Items[0].Meaning = "Power On";
        _viewModel.WordGroups[1].TryAddBit();
        _viewModel.WordGroups[1].Items[0].Meaning = "Motor Running";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task EditBitmapped_LoadsExistingInterpretations()
    {
        // Arrange
        var bitmappedVar = Variable.Restore(
            id: 1,
            name: "ExistingFlags",
            addressHigh: 0x80,
            addressLow: 0x20,
            dataTypeKind: DataTypeKind.Bitmapped,
            dataTypeRaw: "Bitmapped[2]",
            dataTypeParam: 2,
            accessMode: AccessMode.ReadOnly,
            isEnabled: true,
            format: null,
            minValue: null,
            maxValue: null,
            unit: null,
            usage: null,
            description: "Existing flags");
        _variableService.SeedData(bitmappedVar);
        _variableService.SeedBitInterpretations(1,
        [
            BitInterpretation.Restore(1, 1, 0, 0, "Bit 0 Word 0"),
            BitInterpretation.Restore(2, 1, 0, 1, "Bit 1 Word 0"),
            BitInterpretation.Restore(3, 1, 1, 0, "Bit 0 Word 1"),
        ]);

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1);

        // Assert
        Assert.Equal(2, _viewModel.WordGroups.Count);
        Assert.Equal(2, _viewModel.WordGroups[0].Items.Count); // Word 0: 2 bit
        Assert.Single(_viewModel.WordGroups[1].Items); // Word 1: 1 bit
    }
}
#endif
