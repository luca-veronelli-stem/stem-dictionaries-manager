namespace API.Dtos;

/// <summary>
/// Dictionary — list view.
/// </summary>
public record DictionarySummaryDto(
    int Id,
    string Name,
    string? Description,
    bool IsStandard,
    int VariableCount);
