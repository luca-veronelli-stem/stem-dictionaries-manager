namespace Core.Enums;

/// <summary>
/// Data type of a variable.
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

    /// <summary>String with maximum length (e.g. String[20])</summary>
    String,

    /// <summary>Bitmapped value with N words (e.g. "two word uint16_t bitmapped")</summary>
    Bitmapped,

    /// <summary>Array of elements (e.g. "3*uint32_t")</summary>
    Array,

    /// <summary>Unrecognized type (fallback)</summary>
    Other
}
