namespace Core.Enums;

/// <summary>
/// Category of a variable based on its address.
/// </summary>
public enum VariableCategory
{
    /// <summary>Standard variable common to all devices (0x00xx)</summary>
    Standard,

    /// <summary>Variable specific to a device type (0x80xx)</summary>
    DeviceSpecific
}
