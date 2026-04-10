namespace API.Dtos;

/// <summary>
/// Dizionario — dettaglio con variabili proprie (solo abilitate).
/// </summary>
public record DictionaryDetailDto(
    int Id,
    string Name,
    string? Description,
    bool IsStandard,
    IReadOnlyList<VariableDto> Variables);
