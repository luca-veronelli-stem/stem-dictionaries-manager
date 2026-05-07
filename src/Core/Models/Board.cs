namespace Core.Models;

/// <summary>
/// Scheda fisica in un dispositivo.
/// SESSION_035: DeviceType enum → DeviceId FK a Device entity.
/// MachineCode denormalizzato per calcolo ProtocolAddress senza join.
/// DictionaryId opzionale: board senza dizionario (es. SPARK Motore DX).
/// L'indirizzo protocol è calcolato da MACHINE + FIRMWARE_TYPE + BOARD_NUMBER.
/// </summary>
public class Board
{
    public int Id { get; private set; }
    public int DeviceId { get; private set; }
    public string Name { get; private set; }
    public int FirmwareType { get; private set; }
    public int BoardNumber { get; private set; }
    public string? PartNumber { get; private set; }

    /// <summary>
    /// True se è la scheda principale del dispositivo. Max 1 per Device (BR-005).
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Dizionario associato. Null = board senza dizionario proprio.
    /// </summary>
    public int? DictionaryId { get; private set; }

    /// <summary>
    /// Nome del dizionario associato (denormalizzato, read-only, per display).
    /// </summary>
    public string? DictionaryName { get; private set; }

    /// <summary>
    /// Nome del dispositivo (denormalizzato da Device.Name, per display).
    /// </summary>
    public string? DeviceName { get; private set; }

    /// <summary>
    /// MachineCode del dispositivo (denormalizzato da Device.MachineCode, per ProtocolAddress).
    /// </summary>
    public int MachineCode { get; private set; }

    /// <summary>
    /// Indirizzo protocol calcolato.
    /// Formula: (MACHINE &lt;&lt; 16) | ((FIRMWARE_TYPE &amp; 0x03FF) &lt;&lt; 6) | (BOARD_NUMBER &amp; 0x003F)
    /// </summary>
    public uint ProtocolAddress => CalculateAddress(
        MachineCode,
        FirmwareType,
        BoardNumber);

    public Board(int deviceId, string name, int firmwareType, int boardNumber,
        int machineCode, string? partNumber = null, bool isPrimary = false,
        int? dictionaryId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (firmwareType < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(firmwareType),
                "FirmwareType must be non-negative.");
        }

        if (boardNumber < 1 || boardNumber > 63)
        {
            throw new ArgumentOutOfRangeException(nameof(boardNumber),
                "BoardNumber must be between 1 and 63 (BR-008).");
        }

        DeviceId = deviceId;
        Name = name;
        FirmwareType = firmwareType;
        BoardNumber = boardNumber;
        PartNumber = partNumber;
        IsPrimary = isPrimary;
        DictionaryId = dictionaryId;
        MachineCode = machineCode;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Board Restore(int id, int deviceId, string name,
        int firmwareType, int boardNumber, string? partNumber, bool isPrimary,
        int? dictionaryId, int machineCode, string? dictionaryName = null,
        string? deviceName = null)
    {
        var board = new Board(deviceId, name, firmwareType, boardNumber,
            machineCode, partNumber, isPrimary, dictionaryId)
        {
            Id = id,
            DictionaryName = dictionaryName,
            DeviceName = deviceName
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
