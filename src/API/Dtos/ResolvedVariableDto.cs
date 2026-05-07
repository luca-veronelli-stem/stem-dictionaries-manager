namespace API.Dtos;

/// <summary>
/// Resolved variable with standard flag (BR-API-002).
/// </summary>
public record ResolvedVariableDto(
    string Name,
    int AddressHigh,
    int AddressLow,
    string DataType,
    string Access,
    string? Description = null,
    double? Min = null,
    double? Max = null,
    string? Unit = null,
    bool IsStandard = false);
