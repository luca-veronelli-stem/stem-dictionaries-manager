using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Mapper bidirezionale per Board Entity ↔ Domain.
/// </summary>
public static class BoardMapper
{
    /// <summary>
    /// Converte BoardEntity in Board (Domain).
    /// Richiede BoardType già mappato.
    /// </summary>
    public static Board ToDomain(BoardEntity entity, BoardType boardType)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(boardType);

        return Board.Restore(
            entity.Id,
            entity.DeviceType,
            boardType,
            entity.Name,
            entity.BoardNumber,
            entity.PartNumber,
            entity.IsPrimary);
    }

    /// <summary>
    /// Converte BoardEntity in Board (Domain).
    /// Usa navigation property BoardType (deve essere caricata).
    /// </summary>
    public static Board ToDomain(BoardEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity.BoardType == null)
            throw new InvalidOperationException(
                $"BoardType not loaded for Board {entity.Id}. Use Include() or provide BoardType.");

        var boardType = BoardTypeMapper.ToDomain(entity.BoardType);
        return ToDomain(entity, boardType);
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
            BoardTypeId = domain.BoardType.Id,
            Name = domain.Name,
            BoardNumber = domain.BoardNumber,
            PartNumber = domain.PartNumber,
            ProtocolAddress = domain.ProtocolAddress,
            IsPrimary = domain.IsPrimary
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
        entity.BoardTypeId = domain.BoardType.Id;
        entity.Name = domain.Name;
        entity.BoardNumber = domain.BoardNumber;
        entity.PartNumber = domain.PartNumber;
        entity.ProtocolAddress = domain.ProtocolAddress;
        entity.IsPrimary = domain.IsPrimary;
    }

    /// <summary>
    /// Converte lista di entities in lista di domain models.
    /// Richiede BoardType caricato via Include.
    /// </summary>
    public static IReadOnlyList<Board> ToDomainList(IEnumerable<BoardEntity> entities)
    {
        return [.. entities.Select(e => ToDomain(e))];
    }
}
