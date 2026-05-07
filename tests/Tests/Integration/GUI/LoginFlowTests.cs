#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Integration.GUI;

/// <summary>
/// Integration test per il flusso Login (F0).
/// Testa selezione utente e navigazione post-login.
/// </summary>
public class LoginFlowTests
{
    private readonly MockUserService _userService;
    private readonly LoginViewModel _viewModel;

    public LoginFlowTests()
    {
        _userService = new MockUserService();
        _viewModel = new LoginViewModel(_userService);
    }

    [Fact]
    public async Task Login_LoadsUsers()
    {
        // Arrange
        _userService.SeedData(
            User.Restore(1, "user1", "User One"),
            User.Restore(2, "user2", "User Two")
        );

        // Act
        await _viewModel.LoadUsersAsync();

        // Assert
        Assert.Equal(2, _viewModel.AvailableUsers.Count);
    }

    [Fact]
    public async Task Login_WithNoUsers_ShowsErrorMessage()
    {
        // Arrange - nessun utente nel sistema

        // Act
        await _viewModel.LoadUsersAsync();

        // Assert
        Assert.Empty(_viewModel.AvailableUsers);
        Assert.NotNull(_viewModel.ErrorMessage);
    }

    [Fact]
    public async Task Login_SelectUser_EnablesLoginButton()
    {
        // Arrange
        _userService.SeedData(User.Restore(1, "user1", "User One"));
        await _viewModel.LoadUsersAsync();

        // Act
        _viewModel.SelectedUser = _viewModel.AvailableUsers[0];

        // Assert
        Assert.True(_viewModel.ConfirmLoginCommand.CanExecute(null));
    }

    [Fact]
    public async Task Login_ConfirmLogin_RaisesEvent()
    {
        // Arrange
        _userService.SeedData(User.Restore(1, "user1", "User One"));
        await _viewModel.LoadUsersAsync();
        _viewModel.SelectedUser = _viewModel.AvailableUsers[0];

        var eventRaised = false;
        _viewModel.LoginConfirmed += u => eventRaised = true;

        // Act
        _viewModel.ConfirmLoginCommand.Execute(null);

        // Assert
        Assert.True(eventRaised);
    }
}
#endif
