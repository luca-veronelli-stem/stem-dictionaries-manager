namespace API.Dtos;

/// <summary>
/// Board definition — formato compatibile Production.Tracker (BR-API-005).
/// </summary>
public record BoardDefinitionDto(
    string DeviceName,
    string BoardName,
    string BoardAddress,
    int FirmwareType,
    string? Description,
    IReadOnlyList<VariableDto> Variables);
