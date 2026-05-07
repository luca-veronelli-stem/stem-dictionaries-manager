namespace Core.Models;

/// <summary>
/// STEM device (machine).
/// SESSION_035: moved from DeviceType enum to Device entity in the DB.
/// MachineCode is the value used in the protocol address computation.
/// </summary>
public class Device
{
    /// <summary>
    /// MachineCode reserved for the BLE Module (internal component, not a device).
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
        {
            throw new ArgumentOutOfRangeException(nameof(machineCode),
                "MachineCode must be greater than 0 (BR-014).");
        }

        if (machineCode == ReservedBleModuleMachineCode)
        {
            throw new InvalidOperationException(
                $"MachineCode {ReservedBleModuleMachineCode} is reserved for the BLE Module (BR-015).");
        }

        Name = name;
        MachineCode = machineCode;
        Description = description;
    }

    /// <summary>
    /// Factory method to reconstruct from the DB.
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
