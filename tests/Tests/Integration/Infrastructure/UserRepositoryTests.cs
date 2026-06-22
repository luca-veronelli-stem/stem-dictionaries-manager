using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests per UserRepository.
/// </summary>
public class UserRepositoryTests : IntegrationTestBase
{
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _repository = new UserRepository(Context, NullLogger<RepositoryBase<UserEntity>>.Instance);
    }

    [Fact]
    public async Task AddAsync_CreatesUser()
    {
        UserEntity user = TestData.CreateUser("luca", "Luca V.");

        UserEntity result = await _repository.AddAsync(user);

        Assert.True(result.Id > 0);
        Assert.Equal("luca", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser()
    {
        UserEntity user = TestData.CreateUser("test", "Test");
        await _repository.AddAsync(user);

        UserEntity? result = await _repository.GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("test", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        UserEntity? result = await _repository.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsUser()
    {
        UserEntity user = TestData.CreateUser("findme", "Find Me");
        await _repository.AddAsync(user);

        UserEntity? result = await _repository.GetByUsernameAsync("findme");

        Assert.NotNull(result);
        Assert.Equal("Find Me", result.DisplayName);
    }

    [Fact]
    public async Task GetByUsernameAsync_IsCaseInsensitive()
    {
        UserEntity user = TestData.CreateUser("lowercase", "Test");
        await _repository.AddAsync(user);

        UserEntity? result = await _repository.GetByUsernameAsync("LOWERCASE");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        await _repository.AddAsync(TestData.CreateUser("user1", "User 1"));
        await _repository.AddAsync(TestData.CreateUser("user2", "User 2"));

        IReadOnlyList<UserEntity> result = await _repository.GetAllAsync();

        // 2 added here + 1 system-admin seeded via HasData (#88).
        Assert.Equal(3, result.Count);
        Assert.Contains(result, u => u.Username == "user1");
        Assert.Contains(result, u => u.Username == "user2");
    }

    [Fact]
    public async Task UpdateAsync_ModifiesUser()
    {
        UserEntity user = TestData.CreateUser("update", "Before");
        await _repository.AddAsync(user);

        user.DisplayName = "After";
        await _repository.UpdateAsync(user);

        UserEntity? result = await _repository.GetByIdAsync(user.Id);
        Assert.Equal("After", result!.DisplayName);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        UserEntity user = TestData.CreateUser("delete", "Delete Me");
        await _repository.AddAsync(user);

        await _repository.DeleteAsync(user.Id);

        UserEntity? result = await _repository.GetByIdAsync(user.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _repository.DeleteAsync(999));
    }
}
