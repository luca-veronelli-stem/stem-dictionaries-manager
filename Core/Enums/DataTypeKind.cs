namespace Core.Enums;

/// <summary>
/// Tipo di dato di una variabile.
/// </summary>
public enum DataTypeKind
{
    UInt8,
    UInt16,
    UInt32,
    Int8,
    Int16,
    Int32,
    Float,
    Bool,

    /// <summary>Stringa con lunghezza massima (es. String[20])</summary>
    String,

    /// <summary>Valore bitmapped con N word (es. "due word uint16_t bitmapped")</summary>
    Bitmapped,

    /// <summary>Array di elementi (es. "3*uint32_t")</summary>
    Array,

    /// <summary>Tipo non riconosciuto (fallback)</summary>
    Other
}
