namespace Core.Models;

/// <summary>
/// Stato di un comando per un Device specifico (abilitato/disabilitato).
/// SESSION_035: DeviceType enum → DeviceId FK a Device entity.
/// </summary>
public class CommandDeviceState
{
    public int Id { get; private set; }
    public int CommandId { get; private set; }
    public int DeviceId { get; private set; }
    public bool IsEnabled { get; private set; }

    public CommandDeviceState(int commandId, int deviceId, bool isEnabled = true)
    {
        CommandId = commandId;
        DeviceId = deviceId;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static CommandDeviceState Restore(int id, int commandId, int deviceId, bool isEnabled)
    {
        var state = new CommandDeviceState(commandId, deviceId, isEnabled)
        {
            Id = id
        };
        return state;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
