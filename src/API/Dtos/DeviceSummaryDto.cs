namespace API.Dtos;

/// <summary>
/// Device — list view.
/// </summary>
public record DeviceSummaryDto(
    int Id,
    string Name,
    int MachineCode,
    string? Description = null);
