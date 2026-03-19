#if WINDOWS
using Core.Models;
using GUI.Windows.Services;

namespace Tests.Unit.GUI.Services;

/// <summary>
/// Test per CurrentUserService.
/// </summary>
public class CurrentUserServiceTests
{
    [Fact]
    public void IsUserSelected_InitiallyFalse()
    {
        var service = new CurrentUserService();
        Assert.False(service.IsUserSelected);
    }

    [Fact]
    public void CurrentUser_InitiallyNull()
    {
        var service = new CurrentUserService();
        Assert.Null(service.CurrentUser);
    }

    [Fact]
    public void SetCurrentUser_SetsUser()
    {
        var service = new CurrentUserService();
        var user = User.Restore(1, "luca.veronelli", "Luca Veronelli");

        service.SetCurrentUser(user);

        Assert.Equal(user, service.CurrentUser);
        Assert.True(service.IsUserSelected);
    }

    [Fact]
    public void SetCurrentUser_NullUser_ThrowsArgumentNullException()
    {
        var service = new CurrentUserService();

        Assert.Throws<ArgumentNullException>(() => service.SetCurrentUser(null!));
    }

    [Fact]
    public void SetCurrentUser_CanChangeUser()
    {
        var service = new CurrentUserService();
        var user1 = User.Restore(1, "user1", "User One");
        var user2 = User.Restore(2, "user2", "User Two");

        service.SetCurrentUser(user1);
        service.SetCurrentUser(user2);

        Assert.Equal(user2, service.CurrentUser);
    }
}
#endif
