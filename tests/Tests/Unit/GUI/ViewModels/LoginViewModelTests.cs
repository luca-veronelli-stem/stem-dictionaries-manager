#if WINDOWS
using Core.Models;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per LoginViewModel.
/// </summary>
public class LoginViewModelTests
{
    private readonly MockUserService _userService;
    private readonly LoginViewModel _viewModel;

    public LoginViewModelTests()
    {
        _userService = new MockUserService();
        _viewModel = new LoginViewModel(_userService, NullLogger<LoginViewModel>.Instance);
    }

    [Fact]
    public async Task LoadUsersAsync_PopulatesAvailableUsers()
    {
        // Arrange
        _userService.SeedData(
            new User("admin", "Admin"),
            new User("operator", "Operatore"));

        // Act
        await _viewModel.LoadUsersAsync();

        // Assert
        Assert.Equal(2, _viewModel.AvailableUsers.Count);
        Assert.False(_viewModel.IsLoading);
        Assert.Null(_viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadUsersAsync_EmptyDb_SetsErrorMessage()
    {
        // Act
        await _viewModel.LoadUsersAsync();

        // Assert
        Assert.Empty(_viewModel.AvailableUsers);
        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("No users", _viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LoadUsersAsync_OnError_SetsErrorMessage()
    {
        // Arrange
        _userService.ExceptionToThrow = new InvalidOperationException("DB error");

        // Act
        await _viewModel.LoadUsersAsync();

        // Assert
        Assert.NotNull(_viewModel.ErrorMessage);
        Assert.Contains("DB error", _viewModel.ErrorMessage);
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public void ConfirmLoginCommand_CannotExecute_WhenNoUserSelected()
    {
        // Assert
        Assert.False(_viewModel.ConfirmLoginCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmLoginCommand_CanExecute_WhenUserSelected()
    {
        // Arrange
        _viewModel.SelectedUser = User.Restore(1, "admin", "Admin");

        // Assert
        Assert.True(_viewModel.ConfirmLoginCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmLoginCommand_CannotExecute_WhenLoading()
    {
        // Arrange
        _viewModel.SelectedUser = User.Restore(1, "admin", "Admin");
        _viewModel.IsLoading = true;

        // Assert
        Assert.False(_viewModel.ConfirmLoginCommand.CanExecute(null));
    }

    [Fact]
    public void ConfirmLogin_FiresLoginConfirmedEvent()
    {
        // Arrange
        var user = User.Restore(1, "admin", "Admin");
        _viewModel.SelectedUser = user;
        User? confirmedUser = null;
        _viewModel.LoginConfirmed += u => confirmedUser = u;

        // Act
        _viewModel.ConfirmLoginCommand.Execute(null);

        // Assert
        Assert.NotNull(confirmedUser);
        Assert.Equal("Admin", confirmedUser.DisplayName);
    }

    [Fact]
    public void ClearSubscriptions_RemovesEventHandlers()
    {
        // Arrange
        var callCount = 0;
        _viewModel.LoginConfirmed += _ => callCount++;
        _viewModel.SelectedUser = User.Restore(1, "admin", "Admin");

        // Act
        _viewModel.ClearSubscriptions();
        _viewModel.ConfirmLoginCommand.Execute(null);

        // Assert
        Assert.Equal(0, callCount);
    }
}
#endif
