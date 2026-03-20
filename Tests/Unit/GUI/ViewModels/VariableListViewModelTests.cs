#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per VariableListViewModel.
/// </summary>
public class VariableListViewModelTests
{
    private readonly MockVariableService _variableService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly VariableListViewModel _viewModel;

    public VariableListViewModelTests()
    {
        _variableService = new MockVariableService();
        _dictionaryService = new MockDictionaryService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new VariableListViewModel(
            _variableService,
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_LoadsDictionaryName()
    {
        // Arrange
        var dict = new Dictionary("TestDict", description: "Test");
        _dictionaryService.SeedData(dict);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Equal("TestDict", _viewModel.DictionaryName);
    }

    [Fact]
    public async Task InitializeAsync_LoadsVariables()
    {
        // Arrange
        var variable = new Variable("Temp", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadOnly, "uint16_t");
        _variableService.SeedData(variable);
        _dictionaryService.SeedData(new Dictionary("Dict"));

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Single(_viewModel.Variables);
        Assert.Equal("Temp", _viewModel.Variables[0].Name);
    }

    [Fact]
    public async Task InitializeAsync_FormatsAddressCorrectly()
    {
        // Arrange
        var variable = new Variable("Temp", 0x80, 0x01, DataTypeKind.UInt16, AccessMode.ReadOnly, "uint16_t");
        _variableService.SeedData(variable);
        _dictionaryService.SeedData(new Dictionary("Dict"));

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Equal("0x8001", _viewModel.Variables[0].Address);
    }

    [Fact]
    public async Task RefreshCommand_CallsGetByDictionaryIdAsync()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        await _viewModel.InitializeAsync(1);
        _variableService.MethodCalls.Clear();

        // Act
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("GetByDictionaryIdAsync:1", _variableService.MethodCalls);
    }

    [Fact]
    public async Task RefreshCommand_WhenServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        await _viewModel.InitializeAsync(1);
        _variableService.ExceptionToThrow = new Exception("DB error");

        // Act
        await _viewModel.RefreshCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal("DB error", _viewModel.ErrorMessage);
    }

    [Fact]
    public void AddCommand_NavigatesToVariableEdit()
    {
        // Arrange - Set dictionaryId via reflection or initialize
        var field = typeof(VariableListViewModel).GetField("_dictionaryId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(_viewModel, 42);

        // Act
        _viewModel.AddCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.VariableEdit, _navigationService.CurrentView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
        Assert.Equal(42, _navigationService.LastParameter?.ParentId);
    }

    [Fact]
    public void EditCommand_NavigatesToVariableEdit_WithId()
    {
        // Arrange
        var field = typeof(VariableListViewModel).GetField("_dictionaryId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(_viewModel, 1);

        var item = new VariableListItem { Id = 123, Name = "Test" };

        // Act
        _viewModel.EditCommand.Execute(item);

        // Assert
        Assert.Equal(ViewType.VariableEdit, _navigationService.CurrentView);
        Assert.Equal(123, _navigationService.LastParameter?.EntityId);
        Assert.Equal(1, _navigationService.LastParameter?.ParentId);
    }

    [Fact]
    public void EditCommand_WithNull_DoesNotNavigate()
    {
        // Act
        _viewModel.EditCommand.Execute(null);

        // Assert
        Assert.Empty(_navigationService.NavigationHistory);
    }

    [Fact]
    public async Task DeleteCommand_WithConfirmation_DeletesAndRefreshes()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        var variable = new Variable("ToDelete", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t");
        _variableService.SeedData(variable);
        await _viewModel.InitializeAsync(1);
        _dialogService.ConfirmResult = DialogResult.Yes;

        var item = new VariableListItem { Id = 1, Name = "ToDelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.Contains("DeleteAsync:1", _variableService.MethodCalls);
    }

    [Fact]
    public async Task DeleteCommand_WithCancel_DoesNotDelete()
    {
        // Arrange
        _dialogService.ConfirmResult = DialogResult.No;
        var item = new VariableListItem { Id = 1, Name = "ToDelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("DeleteAsync"));
    }

    [Fact]
    public void GoBackCommand_CallsNavigationGoBack()
    {
        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task InitializeAsync_ShowsSuccessMessage()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        var variable = new Variable("Test", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t");
        _variableService.SeedData(variable);

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Contains(_messageService.Messages, m =>
            m.Message.Contains("Caricate") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        await _viewModel.InitializeAsync(1);
        _variableService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync(1);

        // Assert
        Assert.Empty(_variableService.MethodCalls);
    }

    [Fact]
    public async Task SearchText_FiltersListByName()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        _variableService.SeedData(
            new Variable("Temperature", 0x80, 0x01, DataTypeKind.Int16, AccessMode.ReadOnly, "Int16"),
            new Variable("Pressure", 0x80, 0x02, DataTypeKind.Float, AccessMode.ReadOnly, "Float"),
            new Variable("Status", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadOnly, "uint16_t"));
        await _viewModel.InitializeAsync(1);

        // Act
        _viewModel.SearchText = "temp";

        // Assert
        Assert.Single(_viewModel.Variables);
        Assert.Equal("Temperature", _viewModel.Variables[0].Name);
    }

    [Fact]
    public async Task SearchText_FiltersListByAddress()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        _variableService.SeedData(
            new Variable("Var1", 0x80, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new Variable("Var2", 0x00, 0x10, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));
        await _viewModel.InitializeAsync(1);

        // Act
        _viewModel.SearchText = "8001";

        // Assert
        Assert.Single(_viewModel.Variables);
        Assert.Equal("Var1", _viewModel.Variables[0].Name);
    }

    [Fact]
    public async Task SearchText_EmptyString_ShowsAll()
    {
        // Arrange
        _dictionaryService.SeedData(new Dictionary("Dict"));
        _variableService.SeedData(
            new Variable("Var1", 0x00, 0x01, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"),
            new Variable("Var2", 0x00, 0x02, DataTypeKind.UInt8, AccessMode.ReadOnly, "uint8_t"));
        await _viewModel.InitializeAsync(1);
        _viewModel.SearchText = "Var1";
        Assert.Single(_viewModel.Variables);

        // Act
        _viewModel.SearchText = "";

        // Assert
        Assert.Equal(2, _viewModel.Variables.Count);
    }
}
#endif
