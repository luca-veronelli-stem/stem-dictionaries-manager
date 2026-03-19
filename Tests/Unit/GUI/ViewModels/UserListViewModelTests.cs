#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per UserListViewModel.
/// </summary>
public class UserListViewModelTests
{
    private readonly MockUserService _userService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly UserListViewModel _viewModel;

    public UserListViewModelTests()
    {
        _userService = new MockUserService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        _viewModel = new UserListViewModel(
            _userService,
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
        Assert.Contains("GetAllAsync", _userService.MethodCalls);
    }

    [Fact]
    public async Task InitializeAsync_PopulatesUsersList()
    {
        // Arrange
        var user1 = new User("admin", "Administrator");
        var user2 = new User("luca", "Luca Rossi");
        _userService.SeedData(user1, user2);

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Equal(2, _viewModel.Users.Count);
        Assert.Contains(_viewModel.Users, u => u.Username == "admin");
        Assert.Contains(_viewModel.Users, u => u.Username == "luca");
    }

    [Fact]
    public async Task InitializeAsync_ShowsSuccessMessage()
    {
        // Arrange
        _userService.SeedData(new User("test", "Test User"));

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
        _userService.ExceptionToThrow = new Exception("Database error");

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
        _userService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.Empty(_userService.MethodCalls);
    }

    [Fact]
    public void AddCommand_CannotExecute_WhenUsernameEmpty()
    {
        // Arrange
        _viewModel.NewUsername = "";
        _viewModel.NewDisplayName = "Test User";

        // Assert
        Assert.False(_viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public void AddCommand_CannotExecute_WhenDisplayNameEmpty()
    {
        // Arrange
        _viewModel.NewUsername = "test";
        _viewModel.NewDisplayName = "";

        // Assert
        Assert.False(_viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public void AddCommand_CanExecute_WhenValid()
    {
        // Arrange
        _viewModel.NewUsername = "test";
        _viewModel.NewDisplayName = "Test User";

        // Assert
        Assert.True(_viewModel.AddCommand.CanExecute(null));
    }

    [Fact]
    public async Task AddCommand_CallsAddAsync()
    {
        // Arrange
        _viewModel.NewUsername = "newuser";
        _viewModel.NewDisplayName = "New User";

        // Act
        await _viewModel.AddCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_userService.MethodCalls, m => m.StartsWith("AddAsync:newuser"));
    }

    [Fact]
    public async Task AddCommand_OnSuccess_ClearsFields()
    {
        // Arrange
        _viewModel.NewUsername = "newuser";
        _viewModel.NewDisplayName = "New User";

        // Act
        await _viewModel.AddCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(string.Empty, _viewModel.NewUsername);
        Assert.Equal(string.Empty, _viewModel.NewDisplayName);
    }

    [Fact]
    public async Task AddCommand_OnSuccess_ShowsMessage()
    {
        // Arrange
        _viewModel.NewUsername = "newuser";
        _viewModel.NewDisplayName = "New User";

        // Act
        await _viewModel.AddCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m => 
            m.Message.Contains("creato") && m.Severity == MessageSeverity.Success);
    }

    [Fact]
    public async Task AddCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        _viewModel.NewUsername = "newuser";
        _viewModel.NewDisplayName = "New User";
        _userService.ExceptionToThrow = new Exception("Duplicate user");

        // Act
        await _viewModel.AddCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_dialogService.ShowErrorCalled);
    }

    [Fact]
    public async Task DeleteCommand_WithConfirmation_DeletesAndRefreshes()
    {
        // Arrange
        var user = new User("todelete", "To Delete");
        _userService.SeedData(user);
        await _viewModel.InitializeAsync();
        _dialogService.ConfirmResult = DialogResult.Yes;

        var item = new UserListItem { Id = 1, Username = "todelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.Contains("DeleteAsync:1", _userService.MethodCalls);
    }

    [Fact]
    public async Task DeleteCommand_WithCancel_DoesNotDelete()
    {
        // Arrange
        _dialogService.ConfirmResult = DialogResult.No;
        var item = new UserListItem { Id = 1, Username = "todelete" };

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(item);

        // Assert
        Assert.DoesNotContain(_userService.MethodCalls, m => m.StartsWith("DeleteAsync"));
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
