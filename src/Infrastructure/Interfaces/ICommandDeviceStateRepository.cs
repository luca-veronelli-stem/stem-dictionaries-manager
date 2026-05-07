using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface ICommandDeviceStateRepository : IRepository<CommandDeviceStateEntity>
{
    /// <summary>
    /// Gets the state of a command for a specific device.
    /// </summary>
    Task<CommandDeviceStateEntity?> GetByCommandAndDeviceAsync(int commandId, int deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all states for a command.
    /// </summary>
    Task<IReadOnlyList<CommandDeviceStateEntity>> GetByCommandIdAsync(int commandId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all states for a specific device.
    /// </summary>
    Task<IReadOnlyList<CommandDeviceStateEntity>> GetByDeviceIdAsync(int deviceId,
        CancellationToken cancellationToken = default);
}
