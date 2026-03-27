#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

public class DeviceVariablesViewModelTests
{
    private readonly MockVariableService _variableService = new();
    private readonly MockDictionaryService _dictionaryService = new();
    private readonly MockNavigationService _navigationService = new();
    private readonly MockMessageService _messageService = new();
    private readonly DeviceVariablesViewModel _viewModel;

    public DeviceVariablesViewModelTests()
    {
        // Seed dizionario Standard
        _dictionaryService.SeedData(
            Dictionary.Restore(100, "Standard", null, isStandard: true, []));

        _viewModel = new DeviceVariablesViewModel(
            _variableService, _dictionaryService, _navigationService, _messageService);
    }

    // === Defaults ===

    [Fact]
    public void Constructor_DefaultValues()
    {
        Assert.Null(_viewModel.DeviceType);
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
        await _viewModel.LoadAsync(DeviceType.EdenXp);

        Assert.Equal(DeviceType.EdenXp, _viewModel.DeviceType);
        Assert.Equal("Eden-XP", _viewModel.DeviceName);
    }

    [Fact]
    public async Task LoadAsync_PopulatesVariables()
    {
        SeedStandardVariables();

        await _viewModel.LoadAsync(DeviceType.Spark);

        Assert.Equal(2, _viewModel.Variables.Count);
    }

    [Fact]
    public async Task LoadAsync_DefaultIsEnabled_True()
    {
        SeedStandardVariables();

        await _viewModel.LoadAsync(DeviceType.Spark);

        Assert.True(_viewModel.Variables[0].IsEnabled);
        Assert.True(_viewModel.Variables[0].OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_WithOverride_UsesOverrideState()
    {
        SeedStandardVariables();
        var variables = await _variableService.GetByDictionaryIdAsync(100);
        _variableService.SeedDeviceStates(
            new VariableDeviceState(variables[0].Id, DeviceType.Spark, false));

        await _viewModel.LoadAsync(DeviceType.Spark);

        Assert.False(_viewModel.Variables[0].IsEnabled);
        Assert.False(_viewModel.Variables[0].OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_OverrideForOtherDevice_Ignored()
    {
        SeedStandardVariables();
        var variables = await _variableService.GetByDictionaryIdAsync(100);
        _variableService.SeedDeviceStates(
            new VariableDeviceState(variables[0].Id, DeviceType.EdenXp, false));

        await _viewModel.LoadAsync(DeviceType.Spark);

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

        await _viewModel.LoadAsync(DeviceType.Spark);

        var item = _viewModel.Variables[0];
        Assert.True(item.IsGloballyDisabled);
        Assert.False(item.IsEnabled);
        Assert.False(item.OriginalIsEnabled);
    }

    [Fact]
    public async Task LoadAsync_MapsProperties()
    {
        SeedStandardVariables();

        await _viewModel.LoadAsync(DeviceType.Spark);

        var item = _viewModel.Variables[0];
        Assert.Equal("0x0001", item.FullAddress);
        Assert.False(item.IsGloballyDisabled);
    }

    [Fact]
    public async Task LoadAsync_NoStandardDictionary_SetsErrorMessage()
    {
        _dictionaryService.Reset();
        var vm = new DeviceVariablesViewModel(
            _variableService, _dictionaryService, _navigationService, _messageService);

        await vm.LoadAsync(DeviceType.Spark);

        Assert.NotNull(vm.ErrorMessage);
        Assert.Contains("Standard", vm.ErrorMessage);
        Assert.Empty(vm.Variables);
    }

    [Fact]
    public async Task LoadAsync_ServiceThrows_SetsErrorMessage()
    {
        _variableService.ExceptionToThrow = new InvalidOperationException("DB error");

        await _viewModel.LoadAsync(DeviceType.Spark);

        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("DB error", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_IsLoadingFalseAfterCompletion()
    {
        await _viewModel.LoadAsync(DeviceType.Spark);
        Assert.False(_viewModel.IsLoading);
    }

    // === HasChanges ===

    [Fact]
    public async Task HasChanges_NoToggle_False()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);

        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public async Task HasChanges_AfterToggle_True()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);
        _viewModel.Variables[0].IsEnabled = false;

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task HasChanges_ToggleBackToOriginal_False()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);
        _viewModel.Variables[0].IsEnabled = false;
        _viewModel.Variables[0].IsEnabled = true;

        Assert.False(_viewModel.HasChanges);
    }

    // === SaveCommand ===

    [Fact]
    public async Task Save_NoChanges_ShowsInfoMessage()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains("Nessuna modifica", _messageService.CurrentMessage ?? "");
    }

    [Fact]
    public async Task Save_WithChanges_CallsSetDeviceState()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);
        _variableService.MethodCalls.Clear();

        _viewModel.Variables[0].IsEnabled = false;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_variableService.MethodCalls,
            c => c.StartsWith("SetDeviceStateAsync:") && c.Contains("False"));
    }

    [Fact]
    public async Task Save_WithChanges_ShowsSuccessMessage()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);
        _viewModel.Variables[0].IsEnabled = false;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(MessageSeverity.Success, _messageService.CurrentSeverity);
    }

    [Fact]
    public async Task Save_GloballyDisabled_SkipsItem()
    {
        _variableService.SeedData(
            new Variable("Deprecated Var", 0x00, 0x50, DataTypeKind.UInt16,
                AccessMode.ReadOnly, "UInt16",
                isEnabled: false, description: "Deprecata"));

        await _viewModel.LoadAsync(DeviceType.Spark);
        _variableService.MethodCalls.Clear();

        // Anche se cambiamo manualmente (non dovrebbe succedere con UI)
        // il save deve ignorarla perché IsGloballyDisabled
        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_variableService.MethodCalls,
            c => c.StartsWith("SetDeviceStateAsync:"));
    }

    [Fact]
    public async Task Save_ServiceThrows_ShowsErrorMessage()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);
        _viewModel.Variables[0].IsEnabled = false;

        _variableService.ExceptionToThrow = new InvalidOperationException("Save failed");

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(MessageSeverity.Error, _messageService.CurrentSeverity);
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

        await _viewModel.LoadAsync(DeviceType.Spark);

        _viewModel.SelectedVariable = _viewModel.Variables[0];

        Assert.Equal(MessageSeverity.Warning, _messageService.CurrentSeverity);
        Assert.Contains("disattivata globalmente", _messageService.CurrentMessage ?? "");
        Assert.Contains("Deprecated Var", _messageService.CurrentMessage ?? "");
    }

    [Fact]
    public async Task SelectEnabledVariable_DoesNotShowWarning()
    {
        SeedStandardVariables();
        await _viewModel.LoadAsync(DeviceType.Spark);

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
}
#endif
