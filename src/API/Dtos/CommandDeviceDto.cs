namespace API.Dtos;

/// <summary>
/// Command with enable state per device (BR-API-003).
/// </summary>
public record CommandDeviceDto(
    string Name,
    int CodeHigh,
    int CodeLow,
    bool IsResponse,
    IReadOnlyList<string> Parameters);
