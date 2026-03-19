#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per VariableEditViewModel.
/// </summary>
public class VariableEditViewModelTests
{
    private readonly MockVariableService _variableService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly VariableEditViewModel _viewModel;

    public VariableEditViewModelTests()
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
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        // Act
        await _viewModel.InitializeAsync(null, 1);

        // Assert
        Assert.True(_viewModel.IsNew);
        Assert.Equal("Nuova Variabile", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        // Arrange
        var variable = new Variable("Existing", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadWrite, "uint16_t");
        _variableService.SeedData(variable);

        // Act
        await _viewModel.InitializeAsync(1, 1);

        // Assert
        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Variabile", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_LoadsExistingData()
    {
        // Arrange
        var variable = new Variable(
            name: "Temperature",
            addressHigh: 0x80,
            addressLow: 0x10,
            dataTypeKind: DataTypeKind.Int16,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "int16_t",
            isEnabled: true,
            unit: "°C",
            description: "Test var");
        _variableService.SeedData(variable);

        // Act
        await _viewModel.InitializeAsync(1, 1);

        // Assert
        Assert.Equal("Temperature", _viewModel.Name);
        Assert.Equal("80", _viewModel.AddressHighHex);
        Assert.Equal("10", _viewModel.AddressLowHex);
        Assert.Equal(DataTypeKind.Int16, _viewModel.SelectedDataTypeKind);
        Assert.Equal("Int16", _viewModel.DataTypeForSave);
        Assert.Equal(AccessMode.ReadOnly, _viewModel.SelectedAccessMode);
        Assert.Equal("°C", _viewModel.Unit);
        Assert.Equal("Test var", _viewModel.Description);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
        // Act
        await _viewModel.InitializeAsync(999, 1);

        // Assert
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _variableService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync(null, 1);

        // Assert - No additional service calls
        Assert.Empty(_variableService.MethodCalls);
    }

    [Fact]
    public void SaveCommand_CannotExecute_WhenNameEmpty()
    {
        // Arrange
        _viewModel.Name = "";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;

        // Assert
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveCommand_CannotExecute_WhenDataTypeOtherAndCustomEmpty()
    {
        // Arrange
        _viewModel.Name = "TestVar";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Other;
        _viewModel.CustomDataType = "";

        // Assert
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void SaveCommand_CanExecute_WhenNameAndDataTypeSet()
    {
        // Arrange
        _viewModel.Name = "TestVar";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;

        // Assert
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 42);
        _viewModel.Name = "NewVar";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync:42:NewVar"));
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_CallsUpdateAsync()
    {
        // Arrange
        var variable = new Variable("Existing", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadOnly, "uint16_t");
        _variableService.SeedData(variable);
        await _viewModel.InitializeAsync(1, 1);
        _viewModel.Name = "UpdatedName";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("UpdateAsync:1", _variableService.MethodCalls);
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;

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
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;
        _variableService.ExceptionToThrow = new Exception("Save failed");

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
        await _viewModel.InitializeAsync(null, 1);

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CancelCommand_WithChanges_ShowsConfirmDialog()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "Changed"; // This sets HasChanges = true
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.No;

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_dialogService.ShowConfirmCalled);
    }

    [Fact]
    public void FullAddressDisplay_FormatsCorrectly()
    {
        // Arrange
        _viewModel.AddressHighHex = "80";
        _viewModel.AddressLowHex = "01";

        // Assert
        Assert.Equal("0x8001", _viewModel.FullAddressDisplay);
    }

    [Fact]
    public void DataTypeKinds_ContainsAllValues()
    {
        // Assert
        Assert.Equal(Enum.GetValues<DataTypeKind>().Length, _viewModel.DataTypeKinds.Count);
    }

    [Fact]
    public void AccessModes_ContainsAllValues()
    {
        // Assert
        Assert.Equal(Enum.GetValues<AccessMode>().Length, _viewModel.AccessModes.Count);
    }
}
#endif
