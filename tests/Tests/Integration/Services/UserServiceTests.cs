using Core.Models;
using Infrastructure.Repositories;
using Services;

namespace Tests.Integration.Services;

/// <summary>
/// Integration tests per UserService.
/// </summary>
public class UserServiceTests : IntegrationTestBase
{
    private readonly UserService _service;

    public UserServiceTests()
    {
        var repository = new UserRepository(Context);
        _service = new UserService(repository);
    }

    [Fact]
    public async Task AddAsync_ValidUser_CreatesAndReturnsUser()
    {
        // Arrange
        var user = new User("testuser", "Test User");

        // Act
        var result = await _service.AddAsync(user);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("Test User", result.DisplayName);
    }

    [Fact]
    public async Task AddAsync_DuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddAsync(new User("duplicate", "First"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddAsync(new User("duplicate", "Second")));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var created = await _service.AddAsync(new User("findme", "Find Me"));

        // Act
        var result = await _service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("findme", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUser_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        await _service.AddAsync(new User("byname", "By Name"));

        // Act
        var result = await _service.GetByUsernameAsync("byname");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("By Name", result.DisplayName);
    }

    [Fact]
    public async Task GetByUsernameAsync_CaseInsensitive_ReturnsUser()
    {
        // Arrange
        await _service.AddAsync(new User("lowercase", "Test"));

        // Act
        var result = await _service.GetByUsernameAsync("LOWERCASE");

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistingUser_ReturnsNull()
    {
        var result = await _service.GetByUsernameAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        // Arrange
        await _service.AddAsync(new User("user1", "User One"));
        await _service.AddAsync(new User("user2", "User Two"));
        await _service.AddAsync(new User("user3", "User Three"));

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingUser_UpdatesUser()
    {
        // Arrange
        var created = await _service.AddAsync(new User("update", "Before"));
        var updated = User.Restore(created.Id, "update", "After");

        // Act
        await _service.UpdateAsync(updated);

        // Assert
        var result = await _service.GetByIdAsync(created.Id);
        Assert.Equal("After", result!.DisplayName);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        var nonExisting = User.Restore(999, "ghost", "Ghost User");

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateAsync(nonExisting));
    }

    [Fact]
    public async Task UpdateAsync_DuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        await _service.AddAsync(new User("existing", "Existing"));
        var toUpdate = await _service.AddAsync(new User("toupdate", "To Update"));

        var conflicting = User.Restore(toUpdate.Id, "existing", "Conflicting");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(conflicting));
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_ExistingUser_RemovesUser()
    {
        // Arrange
        var created = await _service.AddAsync(new User("delete", "To Delete"));

        // Act
        await _service.DeleteAsync(created.Id);

        // Assert
        var result = await _service.GetByIdAsync(created.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteAsync(999));
    }

    [Fact]
    public async Task UsernameExistsAsync_ExistingUsername_ReturnsTrue()
    {
        // Arrange
        await _service.AddAsync(new User("exists", "Test"));

        // Act
        var result = await _service.UsernameExistsAsync("exists");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UsernameExistsAsync_NonExistingUsername_ReturnsFalse()
    {
        var result = await _service.UsernameExistsAsync("doesnotexist");
        Assert.False(result);
    }
}
