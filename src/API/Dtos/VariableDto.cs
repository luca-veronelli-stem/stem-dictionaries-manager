namespace API.Dtos;

/// <summary>
/// Variable serialized for the API (BR-API-004: nulls omitted).
/// </summary>
public record VariableDto(
    string Name,
    int AddressHigh,
    int AddressLow,
    string DataType,
    string Access,
    string? Description = null,
    double? Min = null,
    double? Max = null,
    string? Unit = null);
