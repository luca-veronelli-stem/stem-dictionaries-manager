#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso DeviceVariables (F5.4).
/// Testa stato variabili standard per device, BR-009/011.
/// </summary>
public class DeviceVariablesFlowTests
{
    private readonly MockVariableService _variableService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockDeviceService _deviceService;
    private readonly MockNavigationService _navigationService;
    private readonly MockMessageService _messageService;
    private readonly DeviceVariablesViewModel _viewModel;

    public DeviceVariablesFlowTests()
    {
        _variableService = new MockVariableService();
        _dictionaryService = new MockDictionaryService();
        _deviceService = new MockDeviceService();
        _navigationService = new MockNavigationService();
        _messageService = new MockMessageService();

        // Seed dati base
        _deviceService.SeedData(Device.Restore(1, "Eden-XP", 3, "Test device"));

        // Dizionario Standard con variabili
        var stdDict = Dictionary.Restore(1, "Standard", null, true, []);
        _dictionaryService.SeedData(stdDict);

        _variableService.SeedData(
            Variable.Restore(1, "FirmwareVersion", 0x00, 0x01,
                Core.Enums.DataTypeKind.UInt16, "UInt16", null,
                Core.Enums.AccessMode.ReadOnly, true,
                null, null, null, null, null, "Versione firmware"),
            Variable.Restore(2, "SerialNumber", 0x00, 0x02,
                Core.Enums.DataTypeKind.String, "String[16]", 16,
                Core.Enums.AccessMode.ReadOnly, true,
                null, null, null, null, null, "Numero di serie"),
            Variable.Restore(3, "DeprecatedVar", 0x00, 0x03,
                Core.Enums.DataTypeKind.UInt8, "UInt8", null,
                Core.Enums.AccessMode.ReadOnly, false, // Deprecata globalmente
                null, null, null, null, null, "Variabile deprecata")
        );

        _viewModel = new DeviceVariablesViewModel(
            _variableService,
            _dictionaryService,
            _deviceService,
            _navigationService,
            _messageService);
    }

    #region Load Tests

    [Fact]
    public async Task LoadVariables_ShowsStandardVariablesOnly()
    {
        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert - mostra tutte le variabili del dizionario standard
        Assert.Equal(3, _viewModel.Variables.Count);
    }

    [Fact]
    public async Task LoadVariables_WithExistingOverrides_ShowsOverrideState()
    {
        // Arrange - override: FirmwareVersion disabilitato per device 1
        _variableService.SeedDeviceStates(
            VariableDeviceState.Restore(1, 1, 1, isEnabled: false)
        );

        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert
        var fwVersion = _viewModel.Variables.First(v => v.Name == "FirmwareVersion");
        Assert.False(fwVersion.IsEnabled);
    }

    [Fact]
    public async Task LoadVariables_GloballyDisabled_ShowsDisabledAndGrayed()
    {
        // Act
        await _viewModel.LoadAsync(deviceId: 1);

        // Assert
        var deprecated = _viewModel.Variables.First(v => v.Name == "DeprecatedVar");
        Assert.True(deprecated.IsGloballyDisabled);
        Assert.False(deprecated.IsEnabled);
    }

    #endregion

    #region Toggle Tests

    [Fact]
    public async Task ToggleVariable_SetsHasChanges()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        Assert.False(_viewModel.HasChanges);

        // Act - toggle una variabile non deprecata
        var fwVersion = _viewModel.Variables.First(v => v.Name == "FirmwareVersion");
        fwVersion.IsEnabled = false;

        // Assert
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task ToggleVariable_BackToOriginal_ClearsHasChanges()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        var fwVersion = _viewModel.Variables.First(v => v.Name == "FirmwareVersion");
        var original = fwVersion.IsEnabled;
        fwVersion.IsEnabled = !original;
        Assert.True(_viewModel.HasChanges);

        // Act
        fwVersion.IsEnabled = original;

        // Assert
        Assert.False(_viewModel.HasChanges);
    }

    #endregion

    #region Save Tests

    [Fact]
    public async Task SaveChanges_OnlyUpdatesModifiedVariables()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        var fwVersion = _viewModel.Variables.First(v => v.Name == "FirmwareVersion");
        fwVersion.IsEnabled = false;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        var setCalls = _variableService.MethodCalls.Where(m => m.StartsWith("SetDeviceStateAsync")).ToList();
        Assert.Single(setCalls);
        Assert.Contains("SetDeviceStateAsync:1:1:False", setCalls);
    }

    [Fact]
    public async Task SaveChanges_SkipsGloballyDisabledVariables()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);

        // La variabile deprecata non dovrebbe essere modificabile,
        // ma verifichiamo che il save non la processi comunque
        var deprecated = _viewModel.Variables.First(v => v.Name == "DeprecatedVar");
        Assert.True(deprecated.IsGloballyDisabled);

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - nessuna chiamata per la variabile deprecata
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.Contains(":3:")); // Id=3
    }

    [Fact]
    public async Task SaveChanges_DisableOnDeprecated_Allowed()
    {
        // Arrange - variabile disabilitata con override già esistente
        _variableService.SeedDeviceStates(
            VariableDeviceState.Restore(1, 3, 1, isEnabled: false)
        );
        await _viewModel.LoadAsync(deviceId: 1);

        // Act - già disabilitata, niente da fare
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - nessun errore
        Assert.DoesNotContain(_messageService.Messages, m => m.Severity == MessageSeverity.Error);
    }

    #endregion

    #region Warning Tests

    [Fact]
    public async Task SelectGloballyDisabled_ShowsWarningMessage()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        var deprecated = _viewModel.Variables.First(v => v.Name == "DeprecatedVar");

        // Act
        _viewModel.SelectedVariable = deprecated;

        // Assert
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
    }

    [Fact]
    public async Task SelectEnabledVariable_NoWarning()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);
        _messageService.Reset();
        var fwVersion = _viewModel.Variables.First(v => v.Name == "FirmwareVersion");

        // Act
        _viewModel.SelectedVariable = fwVersion;

        // Assert
        Assert.DoesNotContain(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
    }

    #endregion

    #region Navigation Tests

    [Fact]
    public async Task GoBack_NavigatesBack()
    {
        // Arrange
        await _viewModel.LoadAsync(deviceId: 1);

        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    #endregion
}
#endif
