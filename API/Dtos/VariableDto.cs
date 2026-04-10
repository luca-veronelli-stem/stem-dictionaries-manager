namespace API.Dtos;

/// <summary>
/// Variabile serializzata per API (BR-API-004: null omessi).
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
