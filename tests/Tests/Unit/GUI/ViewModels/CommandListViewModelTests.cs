#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

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
            _messageService,
            NullLogger<CommandListViewModel>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_CallsGetAllAsync()
    {
        // Act
        await _viewModel.LoadAsync();

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
        await _viewModel.LoadAsync();

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
        await _viewModel.LoadAsync();

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
        await _viewModel.LoadAsync();

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
        await _viewModel.LoadAsync();

        // Assert
        Assert.Contains(_messageService.Messages, m =>
            m.Message.Contains("Loaded") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task InitializeAsync_WhenServiceThrows_SetsErrorMessage()
    {
        // Arrange
        _commandService.ExceptionToThrow = new Exception("Database error");

        // Act
        await _viewModel.LoadAsync();

        // Assert
        Assert.Equal("Database error", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        await _viewModel.LoadAsync();
        _commandService.MethodCalls.Clear();

        // Act — ricarica per aggiornare i dati
        await _viewModel.LoadAsync();

        // Assert — deve ricaricare ogni volta
        Assert.Contains(_commandService.MethodCalls, m => m == "GetAllAsync");
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
    public async Task SearchText_FiltersListByName()
    {
        // Arrange
        _commandService.SeedData(
            new Command("Reset", 0x01, 0x00, false),
            new Command("ReadFW", 0x02, 0x00, false),
            new Command("WriteParam", 0x03, 0x00, false));
        await _viewModel.LoadAsync();

        // Act
        _viewModel.SearchText = "Read";

        // Assert
        Assert.Single(_viewModel.Commands);
        Assert.Equal("ReadFW", _viewModel.Commands[0].Name);
    }

    [Fact]
    public async Task SearchText_FiltersListByCode()
    {
        // Arrange
        _commandService.SeedData(
            new Command("Cmd1", 0x01, 0x00, false),
            new Command("Cmd2", 0x02, 0x00, false));
        await _viewModel.LoadAsync();

        // Act
        _viewModel.SearchText = "0200";

        // Assert
        Assert.Single(_viewModel.Commands);
        Assert.Equal("Cmd2", _viewModel.Commands[0].Name);
    }

    [Fact]
    public async Task SearchText_EmptyString_ShowsAll()
    {
        // Arrange
        _commandService.SeedData(
            new Command("Cmd1", 0x01, 0x00, false),
            new Command("Cmd2", 0x02, 0x00, false));
        await _viewModel.LoadAsync();
        _viewModel.SearchText = "Cmd1";
        Assert.Single(_viewModel.Commands);

        // Act
        _viewModel.SearchText = "";

        // Assert
        Assert.Equal(2, _viewModel.Commands.Count);
    }
}
#endif
