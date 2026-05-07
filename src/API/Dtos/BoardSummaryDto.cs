namespace API.Dtos;

/// <summary>
/// Board — summary used in list and device-detail views.
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
