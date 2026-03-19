#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per CommandListViewModel.
/// </summary>
public class CommandListViewModelTests
{
    private readonly MockCommandService _commandService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly CommandListViewModel _viewModel;

    public CommandListViewModelTests()
    {
        _commandService = new MockCommandService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new CommandListViewModel(
            _commandService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_CallsGetAllAsync()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Contains("GetAllAsync", _commandService.MethodCalls);
    }

    [Fact]
    public async Task InitializeAsync_PopulatesCommandsList()
    {
        // Arrange
        var cmd1 = new Command("ReadTemp", 0x10, 0x01, false);
        var cmd2 = new Command("WriteConfig", 0x20, 0x02, false);
        _commandService.SeedData(cmd1, cmd2);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Equal(2, _viewModel.Commands.Count);
        Assert.Contains(_viewModel.Commands, c => c.Name == "ReadTemp");
        Assert.Contains(_viewModel.Commands, c => c.Name == "WriteConfig");
    }

    [Fact]
    public async Task InitializeAsync_FormatsCodeCorrectly()
    {
        // Arrange
        var command = new Command("TestCmd", 0x12, 0x34, false);
        _commandService.SeedData(command);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Equal("0x1234", _viewModel.Commands[0].Code);
    }

    [Fact]
    public async Task InitializeAsync_SetsIsResponseFlag()
    {
        // Arrange
        var request = new Command("Request", 0x10, 0x01, false);
        var response = new Command("Response", 0x10, 0x02, true);
        _commandService.SeedData(request, response);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Contains(_viewModel.Commands, c => c.Name == "Request" && !c.IsResponse);
        Assert.Contains(_viewModel.Commands, c => c.Name == "Response" && c.IsResponse);
    }

    [Fact]
    public async Task InitializeAsync_ShowsSuccessMessage()
    {
        // Arrange
        _commandService.SeedData(new Command("Test", 0x10, 0x01, false));

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Contains(_messageService.Messages, m => 
            m.Message.Contains("Caricati") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _commandService.ExceptionToThrow = new Exception("Database error");

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Equal("Database error", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        // Arrange
        await _viewModel.InitializeAsync();
        _commandService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Empty(_commandService.MethodCalls);
    }

    [Fact]
    public void AddCommand_NavigatesToCommandEdit()
    {
        // Act
        _viewModel.AddCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.CommandEdit, _navigationService.LastNavigatedView);
        Assert.Null(_navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditCommand_NavigatesToCommandEdit_WithId()
    {
        // Arrange
        var item = new CommandListItem { Id = 42, Name = "TestCmd" };

        // Act
        _viewModel.EditCommand.Execute(item);

        // Assert
        Assert.Equal(ViewType.CommandEdit, _navigationService.LastNavigatedView);
        Assert.Equal(42, _navigationService.LastParameter?.EntityId);
    }

    [Fact]
    public void EditCommand_WithNull_DoesNotNavigate()
    {
        // Act
        _viewModel.EditCommand.Execute(null);

        // Assert
        Assert.Null(_navigationService.LastNavigatedView);
    }

    [Fact]
    public async Task DeleteCommand_WithConfirmation_DeletesAndRefreshes()
    {
        // Arrange
        var command = new Command("ToDelete", 0x10, 0x01, false);
        _commandService.SeedData(command);
        await _viewModel.InitializeAsync();
        _dialogService.ConfirmResult = DialogResult.Yes;

        var item = new CommandListItem { Id = 1, Name = "ToDelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.Contains("DeleteAsync:1", _commandService.MethodCalls);
    }

    [Fact]
    public async Task DeleteCommand_WithCancel_DoesNotDelete()
    {
        // Arrange
        _dialogService.ConfirmResult = DialogResult.No;
        var item = new CommandListItem { Id = 1, Name = "ToDelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.DoesNotContain(_commandService.MethodCalls, m => m.StartsWith("DeleteAsync"));
    }

    [Fact]
    public void GoBackCommand_CallsNavigationGoBack()
    {
        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }
}
#endif
