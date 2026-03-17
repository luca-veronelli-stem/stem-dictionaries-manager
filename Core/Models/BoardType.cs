namespace Core.Models;

/// <summary>
/// Tipo di scheda (raggruppa schede con stesso dizionario).
/// Es: "Madre", "Pulsantiera", "R3lXpMaster", "R3lXpSlave"
/// </summary>
public class BoardType
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public int FirmwareType { get; private set; }

    public BoardType(string name, int firmwareType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (firmwareType < 0)
            throw new ArgumentOutOfRangeException(nameof(firmwareType), "FirmwareType must be non-negative.");

        Name = name;
        FirmwareType = firmwareType;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static BoardType Restore(int id, string name, int firmwareType)
    {
        var boardType = new BoardType(name, firmwareType)
        {
            Id = id
        };
        return boardType;
    }
}
