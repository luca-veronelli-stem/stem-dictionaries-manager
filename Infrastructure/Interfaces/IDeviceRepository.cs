using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IDeviceRepository : IRepository<DeviceEntity>
{
    /// <summary>
    /// Ottiene un device per nome (unique).
    /// </summary>
    Task<DeviceEntity?> GetByNameAsync(string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene un device per MachineCode (unique).
    /// </summary>
    Task<DeviceEntity?> GetByMachineCodeAsync(int machineCode,
        CancellationToken cancellationToken = default);
}
