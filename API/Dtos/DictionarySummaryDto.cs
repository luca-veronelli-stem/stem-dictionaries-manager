namespace API.Dtos;

/// <summary>
/// Dizionario — lista.
/// </summary>
public record DictionarySummaryDto(
    int Id,
    string Name,
    string? Description,
    bool IsStandard,
    int VariableCount);
