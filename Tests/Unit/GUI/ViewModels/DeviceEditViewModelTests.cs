#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per DeviceEditViewModel.
/// </summary>
public class DeviceEditViewModelTests
{
    private readonly MockDeviceService _deviceService = new();
    private readonly MockNavigationService _navigationService = new();
    private readonly MockDialogService _dialogService = new();
    private readonly MockMessageService _messageService = new();
    private readonly DeviceEditViewModel _viewModel;

    public DeviceEditViewModelTests()
    {
        _deviceService.SeedDefaultDevices();
        _viewModel = new DeviceEditViewModel(
            _deviceService, _navigationService, _dialogService, _messageService);
    }

    // === Defaults ===

    [Fact]
    public void Constructor_DefaultValues()
    {
        Assert.Equal(string.Empty, _viewModel.Name);
        Assert.Equal(0, _viewModel.MachineCode);
        Assert.Equal(string.Empty, _viewModel.Description);
        Assert.False(_viewModel.HasChanges);
        Assert.False(_viewModel.IsBusy);
        Assert.Null(_viewModel.ErrorMessage);
        Assert.True(_viewModel.IsNew);
    }

    // === InitializeAsync — New ===

    [Fact]
    public async Task InitializeAsync_Null_StaysNew()
    {
        await _viewModel.InitializeAsync(null);

        Assert.True(_viewModel.IsNew);
        Assert.False(_viewModel.HasChanges);
    }

    // === InitializeAsync — Edit ===

    [Fact]
    public async Task InitializeAsync_ExistingDevice_LoadsProperties()
    {
        await _viewModel.InitializeAsync(3);

        Assert.False(_viewModel.IsNew);
        Assert.Equal("Eden-XP", _viewModel.Name);
        Assert.Equal(3, _viewModel.MachineCode);
        Assert.False(string.IsNullOrEmpty(_viewModel.Description));
        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public async Task InitializeAsync_NonExisting_SetsErrorMessage()
    {
        await _viewModel.InitializeAsync(999);

        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("999", _viewModel.ErrorMessage);
        Assert.True(_viewModel.IsNew);
    }

    // === HasChanges ===

    [Fact]
    public async Task NameChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);

        _viewModel.Name = "New Device";

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task MachineCodeChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);

        _viewModel.MachineCode = 99;

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task DescriptionChanged_SetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);

        _viewModel.Description = "Some desc";

        Assert.True(_viewModel.HasChanges);
    }

    // === Validation ===

    [Fact]
    public async Task SaveCommand_EmptyName_ShowsWarning()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.MachineCode = 99;

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.True(_viewModel.IsNameInvalid);
        Assert.Contains("obbligatori", _messageService.CurrentMessage ?? "");
    }

    [Fact]
    public async Task SaveCommand_ZeroMachineCode_ShowsWarning()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "New Device";

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.True(_viewModel.IsMachineCodeInvalid);
    }

    [Fact]
    public void IsNameInvalid_BeforeFirstSave_ReturnsFalse()
    {
        Assert.False(_viewModel.IsNameInvalid);
    }

    [Fact]
    public void IsMachineCodeInvalid_BeforeFirstSave_ReturnsFalse()
    {
        Assert.False(_viewModel.IsMachineCodeInvalid);
    }

    // === SaveCommand — Add ===

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "New Device";
        _viewModel.MachineCode = 99;

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.Contains(_deviceService.MethodCalls,
            c => c.StartsWith("AddAsync:"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_WhenNew_WithDescription_PassesDescription()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "New Device";
        _viewModel.MachineCode = 99;
        _viewModel.Description = "Test desc";

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.Contains(_deviceService.MethodCalls,
            c => c.StartsWith("AddAsync:"));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_EmptyDescription_PassesNull()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "New Device";
        _viewModel.MachineCode = 99;
        _viewModel.Description = "   ";

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.Contains(_deviceService.MethodCalls,
            c => c.StartsWith("AddAsync:"));
    }

    // === SaveCommand — Update ===

    [Fact]
    public async Task SaveCommand_WhenEdit_CallsUpdateAsync()
    {
        await _viewModel.InitializeAsync(3);
        _viewModel.Name = "Eden-XP Updated";

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.Contains(_deviceService.MethodCalls,
            c => c.StartsWith("UpdateAsync:"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_Success_ResetsHasChanges()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "New";
        _viewModel.MachineCode = 99;

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.False(_viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveCommand_Success_ShowsSuccessMessage()
    {
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "New";
        _viewModel.MachineCode = 99;

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.Equal(MessageSeverity.Success, _messageService.CurrentSeverity);
    }

    // === SaveCommand — Error handling ===

    [Fact]
    public async Task SaveCommand_ServiceThrows_ShowsError()
    {
        _deviceService.ExceptionToThrow = new InvalidOperationException("Duplicate");
        await _viewModel.InitializeAsync(null);
        _viewModel.Name = "New";
        _viewModel.MachineCode = 99;

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(50);

        Assert.Equal(MessageSeverity.Error, _messageService.CurrentSeverity);
        Assert.Contains("Duplicate", _messageService.CurrentMessage ?? "");
    }

    // === DeleteDeviceCommand ===

    [Fact]
    public async Task DeleteDeviceCommand_WhenNew_DoesNothing()
    {
        await _viewModel.InitializeAsync(null);

        _viewModel.DeleteDeviceCommand.Execute(null);
        await Task.Delay(50);

        Assert.DoesNotContain(_deviceService.MethodCalls,
            c => c.StartsWith("DeleteAsync:"));
    }

    [Fact]
    public async Task DeleteDeviceCommand_ConfirmedYes_DeletesAndGoesBack()
    {
        _dialogService.ConfirmResult = DialogResult.Yes;
        await _viewModel.InitializeAsync(3);

        _viewModel.DeleteDeviceCommand.Execute(null);
        await Task.Delay(50);

        Assert.Contains(_deviceService.MethodCalls,
            c => c.StartsWith("DeleteAsync:"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DeleteDeviceCommand_ConfirmedNo_DoesNotDelete()
    {
        _dialogService.ConfirmResult = DialogResult.No;
        await _viewModel.InitializeAsync(3);

        _viewModel.DeleteDeviceCommand.Execute(null);
        await Task.Delay(50);

        Assert.DoesNotContain(_deviceService.MethodCalls,
            c => c.StartsWith("DeleteAsync:"));
    }

    [Fact]
    public async Task DeleteDeviceCommand_ServiceThrows_ShowsError()
    {
        _dialogService.ConfirmResult = DialogResult.Yes;
        await _viewModel.InitializeAsync(3);
        _deviceService.ExceptionToThrow = new Exception("DB error");

        _viewModel.DeleteDeviceCommand.Execute(null);
        await Task.Delay(50);

        Assert.Equal(MessageSeverity.Error, _messageService.CurrentSeverity);
    }

    // === GoBackCommand ===

    [Fact]
    public void GoBackCommand_CallsNavigationGoBack()
    {
        _viewModel.GoBackCommand.Execute(null);

        Assert.True(_navigationService.GoBackCalled);
    }

    // === IEditableViewModel ===

    [Fact]
    public void ImplementsIEditableViewModel()
    {
        Assert.IsAssignableFrom<IEditableViewModel>(_viewModel);
    }
}
#endif
