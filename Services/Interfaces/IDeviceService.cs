using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione dispositivi STEM.
/// SESSION_035: Device entity sostituisce DeviceType enum.
/// </summary>
public interface IDeviceService
{
    Task<Device?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken ct = default);
    Task<Device> AddAsync(Device device, CancellationToken ct = default);
    Task UpdateAsync(Device device, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Cerca device per nome (unique).
    /// </summary>
    Task<Device?> GetByNameAsync(string name, CancellationToken ct = default);
}
