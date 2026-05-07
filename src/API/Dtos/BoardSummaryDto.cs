namespace API.Dtos;

/// <summary>
/// Scheda — summary per lista e dettaglio device.
/// </summary>
public record BoardSummaryDto(
    int Id,
    string Name,
    bool IsPrimary,
    int FirmwareType,
    int BoardNumber,
    string ProtocolAddress,
    int? DictionaryId = null,
    string? DictionaryName = null);
