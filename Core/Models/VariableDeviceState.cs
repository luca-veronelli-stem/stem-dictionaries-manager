namespace Core.Models;

/// <summary>
/// Override per-device dello stato abilitazione di una variabile.
/// Pattern identico a CommandDeviceState.
/// SESSION_035: DeviceType enum → DeviceId FK a Device entity.
/// Se riga presente → override. Se assente → segue Variable.IsEnabled.
/// </summary>
public class VariableDeviceState
{
    public int Id { get; private set; }
    public int VariableId { get; private set; }
    public int DeviceId { get; private set; }
    public bool IsEnabled { get; private set; }

    public VariableDeviceState(int variableId, int deviceId, bool isEnabled = true)
    {
        VariableId = variableId;
        DeviceId = deviceId;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static VariableDeviceState Restore(int id, int variableId, int deviceId, bool isEnabled)
    {
        var state = new VariableDeviceState(variableId, deviceId, isEnabled)
        {
            Id = id
        };
        return state;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
