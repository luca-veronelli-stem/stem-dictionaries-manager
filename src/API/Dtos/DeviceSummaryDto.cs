namespace API.Dtos;

/// <summary>
/// Dispositivo — lista.
/// </summary>
public record DeviceSummaryDto(
    int Id,
    string Name,
    int MachineCode,
    string? Description = null);
