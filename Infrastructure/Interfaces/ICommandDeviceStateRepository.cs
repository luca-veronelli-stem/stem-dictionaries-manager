using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface ICommandDeviceStateRepository : IRepository<CommandDeviceStateEntity>
{
    /// <summary>
    /// Ottiene lo stato di un comando per un device specifico.
    /// </summary>
    Task<CommandDeviceStateEntity?> GetByCommandAndDeviceAsync(int commandId, int deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti gli stati per un comando.
    /// </summary>
    Task<IReadOnlyList<CommandDeviceStateEntity>> GetByCommandIdAsync(int commandId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti gli stati per un device specifico.
    /// </summary>
    Task<IReadOnlyList<CommandDeviceStateEntity>> GetByDeviceIdAsync(int deviceId,
        CancellationToken cancellationToken = default);
}
