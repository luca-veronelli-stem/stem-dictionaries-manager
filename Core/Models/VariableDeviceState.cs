using Core.Enums;

namespace Core.Models;

/// <summary>
/// Override per-device dello stato abilitazione di una variabile.
/// Pattern identico a CommandDeviceState.
/// Se riga presente → override. Se assente → segue Variable.IsEnabled.
/// </summary>
public class VariableDeviceState
{
    public int Id { get; private set; }
    public int VariableId { get; private set; }
    public DeviceType DeviceType { get; private set; }
    public bool IsEnabled { get; private set; }

    public VariableDeviceState(int variableId, DeviceType deviceType, bool isEnabled = true)
    {
        VariableId = variableId;
        DeviceType = deviceType;
        IsEnabled = isEnabled;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static VariableDeviceState Restore(int id, int variableId, DeviceType deviceType, bool isEnabled)
    {
        var state = new VariableDeviceState(variableId, deviceType, isEnabled)
        {
            Id = id
        };
        return state;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
