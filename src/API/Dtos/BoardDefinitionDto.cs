namespace API.Dtos;

/// <summary>
/// Board definition — Production.Tracker-compatible format (BR-API-005).
/// </summary>
public record BoardDefinitionDto(
    string DeviceName,
    string BoardName,
    string BoardAddress,
    int FirmwareType,
    string? Description,
    IReadOnlyList<VariableDto> Variables);
