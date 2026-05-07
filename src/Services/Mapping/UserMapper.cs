using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per User Entity ↔ Domain.
/// </summary>
public static class UserMapper
{
    /// <summary>
    /// Converte UserEntity in User (Domain).
    /// </summary>
    public static User ToDomain(UserEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return User.Restore(entity.Id, entity.Username, entity.DisplayName);
    }

    /// <summary>
    /// Converte User (Domain) in UserEntity per creazione.
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
    /// Aggiorna UserEntity esistente con dati da User (Domain).
    /// Preserva Id e audit fields.
    /// </summary>
    public static void UpdateEntity(UserEntity entity, User domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Username = domain.Username;
        entity.DisplayName = domain.DisplayName;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<User> ToDomainList(IEnumerable<UserEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
