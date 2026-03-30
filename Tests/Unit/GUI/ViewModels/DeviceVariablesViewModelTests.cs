#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Core.Enums;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

public class DeviceVariablesViewModelTests
{
    private readonly MockVariableService _variableService = new();
    private readonly MockDictionaryService _dictionaryService = new();
    private readonly MockDeviceService _deviceService = new();
    private readonly MockNavigationService _navigationService = new();
    private readonly MockMessageService _messageService = new();
    private readonly DeviceVariablesViewModel _viewModel;

    public DeviceVariablesViewModelTests()
    {
        // Seed dizionario Standard
        _dictionaryService.SeedData(
            Dictionary.Restore(100, "Standard", null, isStandard: true, []));
        _deviceService.SeedDefaultDevices();

        _viewModel = new DeviceVariablesViewModel(
            _variableService, _dictionaryService, _deviceService, _navigationService, _messageService);
    }

    // === Defaults ===

    [Fact]
    public void Constructor_DefaultValues()
    {
        Assert.Null(_viewModel.DeviceId);
        Assert.Equal(string.Empty, _viewModel.DeviceName);
        Assert.Empty(_viewModel.Variables);
        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.ErrorMessage);
        Assert.False(_viewModel.HasChanges);
    }

    // === LoadAsync ===

    [Fact]
    public async Task LoadAsync_SetsDeviceTypeAndName()
    {
        await _viewModel.LoadAsync(3);

        Assert.Equal(3, _viewModel.DeviceId);
        Assert.Equal("Eden-XP", _viewModel.DeviceName);
    }

    [Fact]
    public async Task LoadAsync_PopulatesVariables()
    {
        SeedStandardVariables();

        await _viewModel.LoadAsync(7);

        Assert.Equal(2, _viewModel.Variables.Count);
    }

    [Fact]
    public async Task LoadAsync_DefaultIsEnabled_True()
    {
        SeedStandardVariables();

        await _viewModel.LoadAsync(7);

        Assert.True(_viewModel.Variables[0].IsEnabled);
        Assert.True(_viewModel.Variables[0].OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_WithOverride_UsesOverrideState()
    {
        SeedStandardVariables();
        var variables = await _variableService.GetByDictionaryIdAsync(100);
        _variableService.SeedDeviceStates(
            new VariableDeviceState(variables[0].Id, 7, false));

        await _viewModel.LoadAsync(7);

        Assert.False(_viewModel.Variables[0].IsEnabled);
        Assert.False(_viewModel.Variables[0].OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_OverrideForOtherDevice_Ignored()
    {
        SeedStandardVariables();
        var variables = await _variableService.GetByDictionaryIdAsync(100);
        _variableService.SeedDeviceStates(
            new VariableDeviceState(variables[0].Id, 3, false));

        await _viewModel.LoadAsync(7);

        // Spark non ha override, default = true
        Assert.True(_viewModel.Variables[0].IsEnabled);
    }

    [Fact]
    public async Task LoadAsync_GloballyDisabledVariable_ShowsDisabled()
    {
        // Variabile con IsEnabled=false (deprecata globalmente)
        _variableService.SeedData(
            new Variable("Deprecated Var", 0x00, 0x50, DataTypeKind.UInt16,
                AccessMode.ReadOnly, "UInt16",
                isEnabled: false, description: "Deprecata"));

        await _viewModel.LoadAsync(7);

        var item = _viewModel.Variables[0];
        Assert.True(item.IsGloballyDisabled);
        Assert.False(item.IsEnabled);
        Assert.False(item.OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_MapsProperties()
    {
        SeedStandardVariables();

        await _viewModel.LoadAsync(7);

        var item = _viewModel.Variables[0];
        Assert.Equal("0x0001", item.FullAddress);
        Assert.False(item.IsGloballyDisabled);
    }

    [Fact]
    public async Task LoadAsync_NoStandardDictionary_SetsErrorMessage()
    {
        _dictionaryService.Reset();
        var vm = new DeviceVariablesViewModel(
            _variableService, _dictionaryService, _deviceService, _navigationService, _messageService);

        await vm.LoadAsync(7);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("Standard", vm.ErrorMessage);
        Assert.Empty(vm.Variables);
    }

    [Fact]
    public async Task LoadAsync_ServiceThrows_SetsErrorMessage()
    {
        _variableService.ExceptionToThrow = new InvalidOperationException("DB error");

        await _viewModel.LoadAsync(7);

        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("DB error", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingFalseAfterCompletion()
    {
        await _viewModel.LoadAsync(7);
        Assert.False(_viewModel.IsLoading);
    }

    // === HasChanges (read-only list) ===

    [Fact]
    public async Task HasChanges_AlwaysFalse()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(7);

        Assert.False(_viewModel.HasChanges);
    }

    // === GoBack ===

    [Fact]
    public void GoBack_CallsNavigationGoBack()
    {
        _viewModel.GoBackCommand.Execute(null);

        Assert.True(_navigationService.GoBackCalled);
    }

    // === SelectedVariable warning ===

    [Fact]
    public async Task SelectGloballyDisabled_ShowsWarningMessage()
    {
        _variableService.SeedData(
            new Variable("Deprecated Var", 0x00, 0x50, DataTypeKind.UInt16,
                AccessMode.ReadOnly, "UInt16",
                isEnabled: false, description: "Deprecata"));

        await _viewModel.LoadAsync(7);

        _viewModel.SelectedVariable = _viewModel.Variables[0];

        Assert.Equal(MessageSeverity.Warning, _messageService.CurrentSeverity);
        Assert.Contains("disattivata globalmente", _messageService.CurrentMessage ?? "");
        Assert.Contains("Deprecated Var", _messageService.CurrentMessage ?? "");
    }

    [Fact]
    public async Task SelectEnabledVariable_DoesNotShowWarning()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(7);

        _viewModel.SelectedVariable = _viewModel.Variables[0];

        // Nessun warning per variabili attive
        Assert.NotEqual(MessageSeverity.Warning, _messageService.CurrentSeverity);
    }

    // === Helper ===

    private void SeedStandardVariables()
    {
        _variableService.SeedData(
            new Variable("Firmware Version", 0x00, 0x01, DataTypeKind.UInt16,
                AccessMode.ReadOnly, "UInt16",
                isEnabled: true, description: "Versione firmware"),
            new Variable("Serial Number", 0x00, 0x02, DataTypeKind.UInt32,
                AccessMode.ReadOnly, "UInt32",
                isEnabled: true, description: "Numero seriale"));
    }

    // === EditBitInterpretations (doppio click) ===

    [Fact]
    public async Task EditBitInterpretations_BitmappedVariable_NavigatesToVariableEdit()
    {
        _variableService.SeedData(
            new Variable("StatusBits", 0x00, 0x10, DataTypeKind.Bitmapped,
                AccessMode.ReadOnly, "bitmapped[1]",
                isEnabled: true, description: "Status flags"));

        await _viewModel.LoadAsync(7);

        _viewModel.EditBitInterpretationsCommand.Execute(_viewModel.Variables[0]);

        Assert.Equal(ViewType.VariableEdit, _navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task EditBitInterpretations_PassesCorrectParameters()
    {
        _variableService.SeedData(
            new Variable("StatusBits", 0x00, 0x10, DataTypeKind.Bitmapped,
                AccessMode.ReadOnly, "bitmapped[1]",
                isEnabled: true, description: "Status flags"));

        await _viewModel.LoadAsync(7);

        _viewModel.EditBitInterpretationsCommand.Execute(_viewModel.Variables[0]);

        var param = _navigationService.LastParameter;
        Assert.NotNull(param);
        Assert.NotNull(param!.EntityId);        // VariableId
        Assert.NotNull(param.ParentId);          // StandardDictionaryId
        Assert.Equal(7, param.DeviceId);         // DeviceId per DeviceContext mode
    }

    [Fact]
    public async Task EditBitInterpretations_NonBitmapped_StillNavigates()
    {
        SeedStandardVariables(); // UInt16, UInt32

        await _viewModel.LoadAsync(7);

        _viewModel.EditBitInterpretationsCommand.Execute(_viewModel.Variables[0]);

        // Tutte le variabili navigano in DeviceContext (per stato + bit)
        Assert.Equal(ViewType.VariableEdit, _navigationService.LastNavigatedView);
    }

    [Fact]
    public void EditBitInterpretations_NullItem_DoesNotNavigate()
    {
        _viewModel.EditBitInterpretationsCommand.Execute(null);

        Assert.Null(_navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task LoadAsync_BitmappedVariable_SetsIsBitmapped()
    {
        _variableService.SeedData(
            new Variable("StatusBits", 0x00, 0x10, DataTypeKind.Bitmapped,
                AccessMode.ReadOnly, "bitmapped[1]",
                isEnabled: true, description: "Status flags"));

        await _viewModel.LoadAsync(7);

        Assert.True(_viewModel.Variables[0].IsBitmapped);
    }

    [Fact]
    public async Task LoadAsync_NonBitmappedVariable_IsNotBitmapped()
    {
        SeedStandardVariables();

        await _viewModel.LoadAsync(7);

        Assert.False(_viewModel.Variables[0].IsBitmapped);
    }
}
#endif
