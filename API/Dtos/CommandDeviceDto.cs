namespace API.Dtos;

/// <summary>
/// Comando con stato abilitazione per device (BR-API-003).
/// </summary>
public record CommandDeviceDto(
    string Name,
    int CodeHigh,
    int CodeLow,
    bool IsResponse,
    IReadOnlyList<string> Parameters);
