namespace API.Dtos;

/// <summary>
/// Dispositivo — dettaglio con board.
/// </summary>
public record DeviceDetailDto(
    int Id,
    string Name,
    int MachineCode,
    string? Description,
    IReadOnlyList<BoardSummaryDto> Boards);
