using Core.Models;

namespace Tests.Unit.Models;

/// <summary>
/// Test per User model.
/// </summary>
public class UserTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesUser()
    {
        var user = new User("luca", "Luca Veronelli");

        Assert.Equal("luca", user.Username);
        Assert.Equal("Luca Veronelli", user.DisplayName);
        Assert.Equal(0, user.Id);
    }

    [Fact]
    public void Constructor_NormalizesUsernameToLowercase()
    {
        var user = new User("LUCA", "Luca Veronelli");

        Assert.Equal("luca", user.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidUsername_ThrowsArgumentException(string username)
    {
        Assert.Throws<ArgumentException>(() => new User(username, "Display Name"));
    }

    [Fact]
    public void Constructor_NullUsername_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new User(null!, "Display Name"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_InvalidDisplayName_ThrowsArgumentException(string displayName)
    {
        Assert.Throws<ArgumentException>(() => new User("username", displayName));
    }

    [Fact]
    public void Constructor_NullDisplayName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new User("username", null!));
    }

    [Fact]
    public void Restore_SetsIdAndProperties()
    {
        var user = User.Restore(42, "luca", "Luca Veronelli");

        Assert.Equal(42, user.Id);
        Assert.Equal("luca", user.Username);
        Assert.Equal("Luca Veronelli", user.DisplayName);
    }
}
