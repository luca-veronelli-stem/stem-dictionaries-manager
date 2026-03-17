using Core.Enums;

namespace Core.Models;

/// <summary>
/// Variabile nel dizionario.
/// </summary>
public class Variable
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public byte AddressHigh { get; private set; }
    public byte AddressLow { get; private set; }
    public string DataType { get; private set; }
    public string? Format { get; private set; }
    public double? MinValue { get; private set; }
    public double? MaxValue { get; private set; }
    public string? Unit { get; private set; }
    public AccessMode AccessMode { get; private set; }
    public string? Usage { get; private set; }
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Indirizzo completo (AddressHigh << 8 | AddressLow).
    /// </summary>
    public ushort FullAddress => (ushort)((AddressHigh << 8) | AddressLow);

    /// <summary>
    /// Categoria derivata dall'AddressHigh (0x00 = Standard, 0x80 = DeviceSpecific).
    /// </summary>
    public VariableCategory Category => AddressHigh == 0x00 
        ? VariableCategory.Standard 
        : VariableCategory.DeviceSpecific;

    public Variable(
        string name,
        byte addressHigh,
        byte addressLow,
        string dataType,
        AccessMode accessMode,
        bool isEnabled = true,
        string? format = null,
        double? minValue = null,
        double? maxValue = null,
        string? unit = null,
        string? usage = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataType);

        Name = name;
        AddressHigh = addressHigh;
        AddressLow = addressLow;
        DataType = dataType;
        AccessMode = accessMode;
        IsEnabled = isEnabled;
        Format = format;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        Usage = usage;
        Description = description;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Variable Restore(
        int id,
        string name,
        byte addressHigh,
        byte addressLow,
        string dataType,
        AccessMode accessMode,
        bool isEnabled,
        string? format,
        double? minValue,
        double? maxValue,
        string? unit,
        string? usage,
        string? description)
    {
        var variable = new Variable(name, addressHigh, addressLow, dataType, accessMode, 
            isEnabled, format, minValue, maxValue, unit, usage, description)
        {
            Id = id
        };
        return variable;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
