namespace API.Dtos;

/// <summary>
/// Comando protocollo — lista.
/// </summary>
public record CommandDto(
    int Id,
    string Name,
    int CodeHigh,
    int CodeLow,
    bool IsResponse,
    IReadOnlyList<string> Parameters);
