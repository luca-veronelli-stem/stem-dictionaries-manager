using Infrastructure.Entities;
using Infrastructure.Repositories;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per UserRepository.
/// </summary>
public class UserRepositoryTests : IntegrationTestBase
{
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _repository = new UserRepository(Context);
    }

    [Fact]
    public async Task AddAsync_CreatesUser()
    {
        var user = new UserEntity
        {
            Username = "luca",
            DisplayName = "Luca V."
        };

        var result = await _repository.AddAsync(user);

        Assert.True(result.Id > 0);
        Assert.Equal("luca", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser()
    {
        var user = new UserEntity { Username = "test", DisplayName = "Test" };
        await _repository.AddAsync(user);

        var result = await _repository.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("test", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsUser()
    {
        var user = new UserEntity { Username = "findme", DisplayName = "Find Me" };
        await _repository.AddAsync(user);

        var result = await _repository.GetByUsernameAsync("findme");

        Assert.NotNull(result);
        Assert.Equal("Find Me", result.DisplayName);
    }

    [Fact]
    public async Task GetByUsernameAsync_IsCaseInsensitive()
    {
        var user = new UserEntity { Username = "lowercase", DisplayName = "Test" };
        await _repository.AddAsync(user);

        var result = await _repository.GetByUsernameAsync("LOWERCASE");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        await _repository.AddAsync(new UserEntity { Username = "user1", DisplayName = "User 1" });
        await _repository.AddAsync(new UserEntity { Username = "user2", DisplayName = "User 2" });

        var result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task UpdateAsync_ModifiesUser()
    {
        var user = new UserEntity { Username = "update", DisplayName = "Before" };
        await _repository.AddAsync(user);

        user.DisplayName = "After";
        await _repository.UpdateAsync(user);

        var result = await _repository.GetByIdAsync(user.Id);
        Assert.Equal("After", result!.DisplayName);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var user = new UserEntity { Username = "delete", DisplayName = "Delete Me" };
        await _repository.AddAsync(user);

        await _repository.DeleteAsync(user.Id);

        var result = await _repository.GetByIdAsync(user.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }
}
