namespace API.Dtos;

/// <summary>
/// Dictionary — detail with its own variables (enabled only).
/// </summary>
public record DictionaryDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsStandard,
    IReadOnlyList<VariableDto> Variables);
