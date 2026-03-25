#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Services.Interfaces;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso completo di gestione variabili.
/// </summary>
public class VariableEditFlowTests
{
    private readonly MockVariableService _variableService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly VariableEditViewModel _viewModel;

    public VariableEditFlowTests()
    {
        _variableService = new MockVariableService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new VariableEditViewModel(
            _variableService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task CreateVariable_WithValidData_SavesAndNavigatesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "Temperature";
        _viewModel.AddressHighHex = "80";
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
    public async Task CreateBitmappedVariable_RequiresWordCount()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "StatusFlags";
        _viewModel.AddressHighHex = "00";
        _viewModel.AddressLowHex = "20";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = null; // Non impostato

        // Assert - Non può salvare senza WordCount
        Assert.False(_viewModel.SaveCommand.CanExecute(null));

        // Act - Imposta WordCount
        _viewModel.DataTypeParam = 2;

        // Assert - Ora può salvare
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task CreateStringVariable_RequiresSize()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        _viewModel.Name = "DeviceName";
        _viewModel.AddressHighHex = "00";
        _viewModel.AddressLowHex = "30";
        _viewModel.SelectedDataTypeKind = DataTypeKind.String;

        // Assert - Non può salvare senza Size
        Assert.False(_viewModel.SaveCommand.CanExecute(null));

        // Act - Imposta Size
        _viewModel.DataTypeParam = 20;

        // Assert - Ora può salvare
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
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
            id: 2,
            name: "CustomData",
            addressHigh: 0x00,
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

        // Act
        await _viewModel.InitializeAsync(variableId: 2, dictionaryId: 1);

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
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        _viewModel.MinValue = 100;
        _viewModel.MaxValue = 50; // Invalid: min > max

        // Assert
        Assert.False(_viewModel.IsMinMaxValid);
        Assert.False(_viewModel.SaveCommand.CanExecute(null));

        // Act - Fix validation
        _viewModel.MaxValue = 200;

        // Assert
        Assert.True(_viewModel.IsMinMaxValid);
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddressDisplay_UpdatesCorrectly()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        // Assert initial
        Assert.Equal("0x0000", _viewModel.FullAddressDisplay);

        // Act
        _viewModel.AddressHighHex = "80";
        Assert.Equal("0x8000", _viewModel.FullAddressDisplay);

        _viewModel.AddressLowHex = "10";
        Assert.Equal("0x8010", _viewModel.FullAddressDisplay);

        _viewModel.AddressHighHex = "FF";
        _viewModel.AddressLowHex = "FF";
        Assert.Equal("0xFFFF", _viewModel.FullAddressDisplay);
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
        _viewModel.AddressHighHex = "00";
        _viewModel.AddressLowHex = "40";
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
        // Arrange
        var bitmappedVar = Variable.Restore(
            id: 5,
            name: "BitsVar",
            addressHigh: 0x00,
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

        // Seed bit interpretations nel mock
        _variableService.SeedBitInterpretations(5,
        [
            new BitInterpretation(5, 0, 0, "Motor"),
            new BitInterpretation(5, 0, 3, "Pump"),
            new BitInterpretation(5, 1, 0, "Alarm")
        ]);

        // Act
        await _viewModel.InitializeAsync(variableId: 5, dictionaryId: 1);

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

    #region Mock Services

    private class MockVariableService : IVariableService
    {
        private readonly Dictionary<int, Variable> _variables = [];
        private readonly Dictionary<int, List<BitInterpretation>> _bitInterpretations = [];
        private int _nextId = 1;
        public List<string> MethodCalls { get; } = [];
        public Exception? ExceptionToThrow { get; set; }

        public void SeedData(Variable variable)
        {
            _variables[variable.Id] = variable;
            if (variable.Id >= _nextId) _nextId = variable.Id + 1;
        }

        public void SeedBitInterpretations(int variableId, List<BitInterpretation> bits)
        {
            _bitInterpretations[variableId] = bits;
        }

        public Task<Variable?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            MethodCalls.Add($"GetByIdAsync:{id}");
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(_variables.GetValueOrDefault(id));
        }

        public Task<IReadOnlyList<Variable>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Variable>>([.. _variables.Values]);

        public Task<Variable> AddAsync(int dictionaryId, Variable variable, CancellationToken ct = default)
        {
            MethodCalls.Add($"AddAsync:{dictionaryId}:{variable.Name}");
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            var restored = Variable.Restore(_nextId++, variable.Name, variable.AddressHigh, variable.AddressLow,
                variable.DataTypeKind, variable.DataTypeRaw, variable.DataTypeParam, variable.AccessMode,
                variable.IsEnabled, variable.Format, variable.MinValue, variable.MaxValue, variable.Unit,
                variable.Usage, variable.Description);
            _variables[restored.Id] = restored;
            return Task.FromResult(restored);
        }

        public Task UpdateAsync(Variable variable, CancellationToken ct = default)
        {
            MethodCalls.Add($"UpdateAsync:{variable.Id}");
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            _variables[variable.Id] = variable;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken ct = default)
        {
            MethodCalls.Add($"DeleteAsync:{id}");
            _variables.Remove(id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Variable>> GetByDictionaryIdAsync(int dictionaryId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Variable>>([.. _variables.Values]);

        public Task<Variable?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow, CancellationToken ct = default)
            => Task.FromResult<Variable?>(null);

        public Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsAsync(int variableId, CancellationToken ct = default)
        {
            if (_bitInterpretations.TryGetValue(variableId, out var bits))
                return Task.FromResult<IReadOnlyList<BitInterpretation>>(bits);
            return Task.FromResult<IReadOnlyList<BitInterpretation>>([]);
        }

        public Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation, CancellationToken ct = default)
            => Task.FromResult(interpretation);

        public Task UpdateBitInterpretationsAsync(int variableId, IEnumerable<BitInterpretation> interpretations, CancellationToken ct = default)
        {
            MethodCalls.Add($"UpdateBitInterpretationsAsync:{variableId}");
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.CompletedTask;
        }

        public Task SetDeviceStateAsync(int variableId, DeviceType deviceType, bool isEnabled, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<VariableDeviceState?> GetDeviceStateAsync(int variableId, DeviceType deviceType, CancellationToken ct = default)
            => Task.FromResult<VariableDeviceState?>(null);

        public Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesAsync(int variableId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<VariableDeviceState>>([]);
    }

    private class MockNavigationService : INavigationService
    {
        public bool GoBackCalled { get; private set; }
        public ViewType CurrentView => ViewType.VariableEdit;
        public NavigationParameter? CurrentParameter => null;
        public bool CanGoBack => true;
#pragma warning disable CS0067 // Event never used (mock implementation)
        public event EventHandler<ViewType>? CurrentViewChanged;
#pragma warning restore CS0067

        public void NavigateTo(ViewType viewType, NavigationParameter? parameter = null) { }
        public bool GoBack() { GoBackCalled = true; return true; }
    }

    private class MockDialogService : IDialogService
    {
        public bool ShowErrorCalled { get; private set; }
        public bool ShowConfirmCalled { get; private set; }
        public DialogResult ConfirmResult { get; set; } = DialogResult.Yes;

        public Task ShowErrorAsync(string title, string message)
        {
            ShowErrorCalled = true;
            return Task.CompletedTask;
        }

        public Task<DialogResult> ShowConfirmAsync(string title, string message)
        {
            ShowConfirmCalled = true;
            return Task.FromResult(ConfirmResult);
        }

        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;
        public Task ShowWarningAsync(string title, string message) => Task.CompletedTask;
        public Task<DialogResult> ShowOkCancelAsync(string title, string message) => Task.FromResult(DialogResult.Ok);
    }

    private class MockMessageService : IMessageService
    {
        public List<(string Message, MessageSeverity Severity)> Messages { get; } = [];
        public string? CurrentMessage => Messages.LastOrDefault().Message;
        public MessageSeverity CurrentSeverity => Messages.LastOrDefault().Severity;
#pragma warning disable CS0067 // Event never used (mock implementation)
        public event EventHandler? MessageChanged;
#pragma warning restore CS0067

        public void Show(string message, MessageSeverity severity = MessageSeverity.Info)
            => Messages.Add((message, severity));
        public void Show(string message, MessageSeverity severity, int durationMs)
            => Messages.Add((message, severity));
        public void Clear() => Messages.Clear();
    }

    #endregion
}
#endif
