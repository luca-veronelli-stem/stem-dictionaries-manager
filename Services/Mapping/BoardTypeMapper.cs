using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per BoardType Entity ↔ Domain.
/// </summary>
public static class BoardTypeMapper
{
    /// <summary>
    /// Converte BoardTypeEntity in BoardType (Domain).
    /// </summary>
    public static BoardType ToDomain(BoardTypeEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return BoardType.Restore(entity.Id, entity.Name, entity.FirmwareType);
    }

    /// <summary>
    /// Converte BoardType (Domain) in BoardTypeEntity per creazione.
    /// </summary>
    public static BoardTypeEntity ToEntity(BoardType domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new BoardTypeEntity
        {
            Id = domain.Id,
            Name = domain.Name,
            FirmwareType = domain.FirmwareType
        };
    }

    /// <summary>
    /// Aggiorna BoardTypeEntity esistente con dati da BoardType (Domain).
    /// </summary>
    public static void UpdateEntity(BoardTypeEntity entity, BoardType domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.Name = domain.Name;
        entity.FirmwareType = domain.FirmwareType;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<BoardType> ToDomainList(IEnumerable<BoardTypeEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
