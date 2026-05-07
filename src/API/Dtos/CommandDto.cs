namespace API.Dtos;

/// <summary>
/// Protocol command — list view.
/// </summary>
public record CommandDto(
    int Id,
    string Name,
    int CodeHigh,
    int CodeLow,
    bool IsResponse,
    IReadOnlyList<string> Parameters);
