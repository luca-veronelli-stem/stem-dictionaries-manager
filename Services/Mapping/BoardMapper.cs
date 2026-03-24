using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per Board Entity ↔ Domain.
/// Domain v2: nessun BoardType, FirmwareType diretto + DictionaryId?.
/// </summary>
public static class BoardMapper
{
    /// <summary>
    /// Converte BoardEntity in Board (Domain).
    /// </summary>
    public static Board ToDomain(BoardEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Board.Restore(
            entity.Id,
            entity.DeviceType,
            entity.Name,
            entity.FirmwareType,
            entity.BoardNumber,
            entity.PartNumber,
            entity.IsPrimary,
            entity.DictionaryId);
    }

    /// <summary>
    /// Converte Board (Domain) in BoardEntity per creazione.
    /// </summary>
    public static BoardEntity ToEntity(Board domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new BoardEntity
        {
            Id = domain.Id,
            DeviceType = domain.DeviceType,
            Name = domain.Name,
            FirmwareType = domain.FirmwareType,
            BoardNumber = domain.BoardNumber,
            PartNumber = domain.PartNumber,
            ProtocolAddress = domain.ProtocolAddress,
            IsPrimary = domain.IsPrimary,
            DictionaryId = domain.DictionaryId
        };
    }

    /// <summary>
    /// Aggiorna BoardEntity esistente con dati da Board (Domain).
    /// </summary>
    public static void UpdateEntity(BoardEntity entity, Board domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.DeviceType = domain.DeviceType;
        entity.Name = domain.Name;
        entity.FirmwareType = domain.FirmwareType;
        entity.BoardNumber = domain.BoardNumber;
        entity.PartNumber = domain.PartNumber;
        entity.ProtocolAddress = domain.ProtocolAddress;
        entity.IsPrimary = domain.IsPrimary;
        entity.DictionaryId = domain.DictionaryId;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// </summary>
    public static IReadOnlyList<Board> ToDomainList(IEnumerable<BoardEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
