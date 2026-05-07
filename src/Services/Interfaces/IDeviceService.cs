using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// STEM device service.
/// SESSION_035: Device entity replaces DeviceType enum.
/// </summary>
public interface IDeviceService
{
    Task<Device?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken ct = default);
    Task<Device> AddAsync(Device device, CancellationToken ct = default);
    Task UpdateAsync(Device device, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Looks up a device by name (unique).
    /// </summary>
    Task<Device?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Computes the first available MachineCode (max + 1, skipping 6 reserved for BLE).
    /// </summary>
    Task<int> GetNextAvailableMachineCodeAsync(CancellationToken ct = default);
}
