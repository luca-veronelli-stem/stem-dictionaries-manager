namespace Core.Models;

/// <summary>
/// Dispositivo STEM (macchina).
/// SESSION_035: da DeviceType enum a Device entity nel DB.
/// MachineCode è il valore usato nel calcolo dell'indirizzo protocol.
/// </summary>
public class Device
{
    /// <summary>
    /// MachineCode riservato per BLE Module (componente interna, non un device).
    /// </summary>
    public const int ReservedBleModuleMachineCode = 6;

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int MachineCode { get; private set; }

    public Device(string name, int machineCode, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (machineCode <= 0)
            throw new ArgumentOutOfRangeException(nameof(machineCode),
                "MachineCode must be greater than 0 (BR-014).");
        if (machineCode == ReservedBleModuleMachineCode)
            throw new InvalidOperationException(
                $"MachineCode {ReservedBleModuleMachineCode} è riservato per BLE Module (BR-015).");

        Name = name;
        MachineCode = machineCode;
        Description = description;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Device Restore(int id, string name, int machineCode, string? description)
    {
        var device = new Device(name, machineCode, description)
        {
            Id = id
        };
        return device;
    }
}
