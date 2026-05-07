using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IDeviceRepository : IRepository<DeviceEntity>
{
    /// <summary>
    /// Gets a device by name (unique).
    /// </summary>
    Task<DeviceEntity?> GetByNameAsync(string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a device by MachineCode (unique).
    /// </summary>
    Task<DeviceEntity?> GetByMachineCodeAsync(int machineCode,
        CancellationToken cancellationToken = default);
}
