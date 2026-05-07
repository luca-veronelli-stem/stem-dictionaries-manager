using Core.Models;
using Infrastructure.Entities;
using Services.Mapping;

namespace Tests.Unit.Services.Mapping;

/// <summary>
/// Unit tests per UserMapper.
/// </summary>
public class UserMapperTests
{
    [Fact]
    public void ToDomain_ValidEntity_ReturnsUser()
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = 1,
            Username = "luca",
            DisplayName = "Luca V."
        };

        // Act
        User result = UserMapper.ToDomain(entity);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("luca", result.Username);
        Assert.Equal("Luca V.", result.DisplayName);
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UserMapper.ToDomain(null!));
    }

    [Fact]
    public void ToEntity_ValidDomain_ReturnsEntity()
    {
        // Arrange
        var user = User.Restore(5, "admin", "Administrator");

        // Act
        UserEntity result = UserMapper.ToEntity(user);

        // Assert
        Assert.Equal(5, result.Id);
        Assert.Equal("admin", result.Username);
        Assert.Equal("Administrator", result.DisplayName);
    }

    [Fact]
    public void ToEntity_NullDomain_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UserMapper.ToEntity(null!));
    }

    [Fact]
    public void UpdateEntity_ValidInputs_UpdatesAllFields()
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = 1,
            Username = "old_user",
            DisplayName = "Old Name"
        };
        var domain = User.Restore(1, "new_user", "New Name");

        // Act
        UserMapper.UpdateEntity(entity, domain);

        // Assert
        Assert.Equal("new_user", entity.Username);
        Assert.Equal("New Name", entity.DisplayName);
        Assert.Equal(1, entity.Id); // Id non cambia
    }

    [Fact]
    public void UpdateEntity_NullEntity_ThrowsArgumentNullException()
    {
        var domain = new User("test", "Test");
        Assert.Throws<ArgumentNullException>(() => UserMapper.UpdateEntity(null!, domain));
    }

    [Fact]
    public void UpdateEntity_NullDomain_ThrowsArgumentNullException()
    {
        var entity = new UserEntity { Id = 1, Username = "test", DisplayName = "Test" };
        Assert.Throws<ArgumentNullException>(() => UserMapper.UpdateEntity(entity, null!));
    }

    [Fact]
    public void ToDomainList_MultipleEntities_ReturnsAllMapped()
    {
        // Arrange
        var entities = new List<UserEntity>
        {
            new() { Id = 1, Username = "user1", DisplayName = "User One" },
            new() { Id = 2, Username = "user2", DisplayName = "User Two" },
            new() { Id = 3, Username = "user3", DisplayName = "User Three" }
        };

        // Act
        IReadOnlyList<User> result = UserMapper.ToDomainList(entities);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("user1", result[0].Username);
        Assert.Equal("user2", result[1].Username);
        Assert.Equal("user3", result[2].Username);
    }

    [Fact]
    public void ToDomainList_EmptyList_ReturnsEmptyList()
    {
        IReadOnlyList<User> result = UserMapper.ToDomainList([]);
        Assert.Empty(result);
    }

    [Fact]
    public void RoundTrip_EntityToDomainToEntity_PreservesData()
    {
        // Arrange
        var originalEntity = new UserEntity
        {
            Id = 42,
            Username = "roundtrip",
            DisplayName = "Round Trip User"
        };

        // Act
        User domain = UserMapper.ToDomain(originalEntity);
        UserEntity resultEntity = UserMapper.ToEntity(domain);

        // Assert
        Assert.Equal(originalEntity.Id, resultEntity.Id);
        Assert.Equal(originalEntity.Username, resultEntity.Username);
        Assert.Equal(originalEntity.DisplayName, resultEntity.DisplayName);
    }
}
