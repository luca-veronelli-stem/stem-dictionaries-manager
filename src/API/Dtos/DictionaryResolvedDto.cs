namespace API.Dtos;

/// <summary>
/// Dizionario con variabili risolte: standard (con override BR-009/020) + specifiche (BR-API-002).
/// </summary>
public record DictionaryResolvedDto(
    int Id,
    string Name,
    string? Description,
    IReadOnlyList<ResolvedVariableDto> Variables);
