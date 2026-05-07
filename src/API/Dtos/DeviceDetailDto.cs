namespace API.Dtos;

/// <summary>
/// Device — detail with boards.
/// </summary>
public record DeviceDetailDto(
    int Id,
    string Name,
    int MachineCode,
    string? Description,
    IReadOnlyList<BoardSummaryDto> Boards);
