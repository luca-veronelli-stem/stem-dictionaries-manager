using Core.Enums;

namespace Core.Models;

/// <summary>
/// Stato di un comando per un DeviceType specifico (abilitato/disabilitato).
/// </summary>
public class CommandDeviceState
{
    public int Id { get; private set; }
    public int CommandId { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public bool IsEnabled { get; private set; }

    public CommandDeviceState(int commandId, DeviceType deviceType, bool isEnabled = true)
    {
        CommandId = commandId;
        DeviceType = deviceType;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static CommandDeviceState Restore(int id, int commandId, DeviceType deviceType, bool isEnabled)
    {
        var state = new CommandDeviceState(commandId, deviceType, isEnabled)
        {
            Id = id
        };
        return state;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
