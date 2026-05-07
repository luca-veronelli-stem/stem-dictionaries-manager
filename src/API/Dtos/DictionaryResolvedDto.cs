namespace API.Dtos;

/// <summary>
/// Dictionary with resolved variables: standard (with overrides BR-009/020) + device-specific (BR-API-002).
/// </summary>
public record DictionaryResolvedDto(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<ResolvedVariableDto> Variables);
