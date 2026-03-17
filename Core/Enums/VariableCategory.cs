namespace Core.Enums;

/// <summary>
/// Categoria di una variabile in base all'indirizzo.
/// </summary>
public enum VariableCategory
{
    /// <summary>Variabile standard comune a tutti i device (0x00xx)</summary>
    Standard,
    
    /// <summary>Variabile specifica per tipo di device (0x80xx)</summary>
    DeviceSpecific
}
