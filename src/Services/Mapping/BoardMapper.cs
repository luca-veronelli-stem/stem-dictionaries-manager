using Core.Models;
using Infrastructure.Entities;

namespace Services.Mapping;

/// <summary>
/// Bidirectional mapper for Board Entity ↔ Domain.
/// SESSION_035: DeviceType → DeviceId + DeviceName + MachineCode.
/// </summary>
public static class BoardMapper
{
    public static Board ToDomain(BoardEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Board.Restore(
            entity.Id,
            entity.DeviceId,
            entity.Name,
            entity.FirmwareType,
            entity.BoardNumber,
            entity.PartNumber,
            entity.IsPrimary,
            entity.DictionaryId,
            entity.Device?.MachineCode
                ?? throw new InvalidOperationException(
                    $"Board '{entity.Name}' (Id={entity.Id}) has no Device loaded. " +
                    "Include Device in the query to provide MachineCode."),
            entity.Dictionary?.Name,
            entity.Device?.Name);
    }

    public static BoardEntity ToEntity(Board domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new BoardEntity
        {
            Id = domain.Id,
            DeviceId = domain.DeviceId,
            Name = domain.Name,
            FirmwareType = domain.FirmwareType,
            BoardNumber = domain.BoardNumber,
            PartNumber = domain.PartNumber,
            ProtocolAddress = domain.ProtocolAddress,
            IsPrimary = domain.IsPrimary,
            DictionaryId = domain.DictionaryId
        };
    }

    public static void UpdateEntity(BoardEntity entity, Board domain)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(domain);

        entity.DeviceId = domain.DeviceId;
        entity.Name = domain.Name;
        entity.FirmwareType = domain.FirmwareType;
        entity.BoardNumber = domain.BoardNumber;
        entity.PartNumber = domain.PartNumber;
        entity.ProtocolAddress = domain.ProtocolAddress;
        entity.IsPrimary = domain.IsPrimary;
        entity.DictionaryId = domain.DictionaryId;
    }

    public static IReadOnlyList<Board> ToDomainList(IEnumerable<BoardEntity> entities)
    {
        return [.. entities.Select(ToDomain)];
    }
}
