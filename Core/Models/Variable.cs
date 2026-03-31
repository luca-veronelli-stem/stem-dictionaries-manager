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

    /// <summary>Tipo di dato strutturato.</summary>
    public DataTypeKind DataTypeKind { get; private set; }

    /// <summary>Parametro opzionale per il tipo (es. 20 per String[20], wordCount per Bitmapped).</summary>
    public int? DataTypeParam { get; private set; }

    /// <summary>Valore originale del tipo dall'Excel (per export e riferimento).</summary>
    public string DataTypeRaw { get; private set; }

    public string? Format { get; private set; }
    public double? MinValue { get; private set; }
    public double? MaxValue { get; private set; }
    public string? Unit { get; private set; }
    public AccessMode AccessMode { get; private set; }
    public string? Usage { get; private set; }
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Dimensione in bit di ogni word per variabili Bitmapped (8, 16 o 32).
    /// Null per tutti gli altri tipi. BR-019.
    /// </summary>
    public int? WordSize { get; private set; }

    /// <summary>Valori ammessi per WordSize (BR-019).</summary>
    public static readonly int[] AllowedWordSizes = [8, 16, 32];

    /// <summary>
    /// Indirizzo completo (AddressHigh << 8 | AddressLow).
    /// </summary>
    public ushort FullAddress => (ushort)((AddressHigh << 8) | AddressLow);

    /// <summary>
    /// Categoria derivata dall'AddressHigh (0x00 = Standard, 0x80 = DeviceSpecific).
    /// </summary>
    public VariableCategory Category => AddressHigh switch
    {
        0x00 => VariableCategory.Standard,
        0x80 => VariableCategory.DeviceSpecific,
        _ => throw new InvalidOperationException($"Unknown AddressHigh: 0x{AddressHigh:X2}")
    };

    public Variable(
        string name,
        byte addressHigh,
        byte addressLow,
        DataTypeKind dataTypeKind,
        AccessMode accessMode,
        string dataTypeRaw,
        int? dataTypeParam = null,
        bool isEnabled = true,
        string? format = null,
        double? minValue = null,
        double? maxValue = null,
        string? unit = null,
        string? usage = null,
        string? description = null,
        int? wordSize = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataTypeRaw);

        if (addressHigh != 0x00 && addressHigh != 0x80)
            throw new ArgumentOutOfRangeException(nameof(addressHigh),
                $"AddressHigh must be 0x00 (Standard) or 0x80 (DeviceSpecific), got 0x{addressHigh:X2}.");

        if (wordSize.HasValue && !AllowedWordSizes.Contains(wordSize.Value))
            throw new ArgumentOutOfRangeException(nameof(wordSize),
                $"WordSize must be 8, 16 or 32, got {wordSize.Value}.");

        Name = name;
        AddressHigh = addressHigh;
        AddressLow = addressLow;
        DataTypeKind = dataTypeKind;
        DataTypeParam = dataTypeParam;
        DataTypeRaw = dataTypeRaw;
        AccessMode = accessMode;
        IsEnabled = isEnabled;
        Format = format;
        MinValue = minValue;
        MaxValue = maxValue;
        Unit = unit;
        Usage = usage;
        Description = description;
        WordSize = wordSize;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Variable Restore(
        int id,
        string name,
        byte addressHigh,
        byte addressLow,
        DataTypeKind dataTypeKind,
        string dataTypeRaw,
        int? dataTypeParam,
        AccessMode accessMode,
        bool isEnabled,
        string? format,
        double? minValue,
        double? maxValue,
        string? unit,
        string? usage,
        string? description,
        int? wordSize = null)
    {
        var variable = new Variable(name, addressHigh, addressLow, dataTypeKind, accessMode,
            dataTypeRaw, dataTypeParam, isEnabled, format, minValue, maxValue, unit, usage,
            description, wordSize)
        {
            Id = id
        };
        return variable;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}
