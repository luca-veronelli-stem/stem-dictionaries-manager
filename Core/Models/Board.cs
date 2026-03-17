using Core.Enums;

namespace Core.Models;

/// <summary>
/// Scheda fisica in un dispositivo.
/// L'indirizzo protocol è calcolato da MACHINE + FIRMWARE_TYPE + BOARD_NUMBER.
/// </summary>
public class Board
{
    public int Id { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public BoardType BoardType { get; private set; }
    public string Name { get; private set; }
    public int BoardNumber { get; private set; }
    public string? PartNumber { get; private set; }

    /// <summary>
    /// Indirizzo protocol calcolato.
    /// Formula: (MACHINE << 16) | ((FIRMWARE_TYPE & 0x03FF) << 6) | (BOARD_NUMBER & 0x003F)
    /// </summary>
    public uint ProtocolAddress => CalculateAddress(
        (int)DeviceType,
        BoardType.FirmwareType,
        BoardNumber);

    public Board(DeviceType deviceType, BoardType boardType, string name, int boardNumber, string? partNumber = null)
    {
        ArgumentNullException.ThrowIfNull(boardType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (boardNumber < 1 || boardNumber > 63)
            throw new ArgumentOutOfRangeException(nameof(boardNumber), "BoardNumber must be between 1 and 63.");

        DeviceType = deviceType;
        BoardType = boardType;
        Name = name;
        BoardNumber = boardNumber;
        PartNumber = partNumber;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Board Restore(int id, DeviceType deviceType, BoardType boardType, 
        string name, int boardNumber, string? partNumber)
    {
        var board = new Board(deviceType, boardType, name, boardNumber, partNumber)
        {
            Id = id
        };
        return board;
    }

    /// <summary>
    /// Calcola l'indirizzo protocol STEM.
    /// </summary>
    public static uint CalculateAddress(int machineCode, int firmwareType, int boardNumber)
    {
        return (uint)(
            (machineCode << 16) |
            ((firmwareType & 0x03FF) << 6) |
            (boardNumber & 0x003F));
    }
}
