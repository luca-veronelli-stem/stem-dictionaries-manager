namespace Core.Models;

/// <summary>
/// Physical board in a device.
/// SESSION_035: DeviceType enum → DeviceId FK to Device entity.
/// MachineCode denormalized to compute ProtocolAddress without a join.
/// DictionaryId optional: board with no dictionary (e.g. SPARK Right Motor).
/// The protocol address is computed from MACHINE + FIRMWARE_TYPE + BOARD_NUMBER.
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
    /// True if this is the device's primary board. Max 1 per Device (BR-005).
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Associated dictionary. Null = board without its own dictionary.
    /// </summary>
    public int? DictionaryId { get; private set; }

    /// <summary>
    /// Name of the associated dictionary (denormalized, read-only, for display).
    /// </summary>
    public string? DictionaryName { get; private set; }

    /// <summary>
    /// Device name (denormalized from Device.Name, for display).
    /// </summary>
    public string? DeviceName { get; private set; }

    /// <summary>
    /// Device MachineCode (denormalized from Device.MachineCode, for ProtocolAddress).
    /// </summary>
    public int MachineCode { get; private set; }

    /// <summary>
    /// Computed protocol address.
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
    /// Factory method to reconstruct from the DB.
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
    /// Computes the STEM protocol address.
    /// </summary>
    public static uint CalculateAddress(int machineCode, int firmwareType, int boardNumber)
    {
        return (uint)(
            (machineCode << 16) |
            ((firmwareType & 0x03FF) << 6) |
            (boardNumber & 0x003F));
    }
}
