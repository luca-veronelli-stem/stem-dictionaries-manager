namespace Core.Enums;

/// <summary>
/// Permessi di accesso per una variabile.
/// </summary>
public enum AccessMode
{
    /// <summary>Sola lettura (R)</summary>
    ReadOnly,

    /// <summary>Lettura e scrittura (RW)</summary>
    ReadWrite,

    /// <summary>Sola scrittura (W)</summary>
    WriteOnly
}
