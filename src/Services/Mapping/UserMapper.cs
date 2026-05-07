using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for User Entity ↔ Domain.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Converts UserEntity to User (Domain).
    /// </summary>
    public static User ToDomain(UserEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return User.Restore(entity.Id, entity.Username, entity.DisplayName);
    }

    /// <summary>
    /// Converts User (Domain) to UserEntity for creation.
    /// </summary>
    public static UserEntity ToEntity(User domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new UserEntity
        {
            Id = domain.Id,
            Username = domain.Username,
            DisplayName = domain.DisplayName
        };
    }

    /// <summary>
    /// Updates an existing UserEntity with data from User (Domain).
    /// Preserves Id and audit fields.
    /// </summary>
    public static void UpdateEntity(UserEntity entity, User domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Username = domain.Username;
        entity.DisplayName = domain.DisplayName;
    }

    /// <summary>
    /// Converts a list of entities to a list of domain models.
    /// </summary>
    public static IReadOnlyList<User> ToDomainList(IEnumerable<UserEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
