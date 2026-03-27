using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IVariableDeviceStateRepository : IRepository<VariableDeviceStateEntity>
{
    /// <summary>
    /// Ottiene lo stato di una variabile per un device specifico.
    /// </summary>
    Task<VariableDeviceStateEntity?> GetByVariableAndDeviceAsync(int variableId, int deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti gli stati per una variabile.
    /// </summary>
    Task<IReadOnlyList<VariableDeviceStateEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti gli stati per un device.
    /// </summary>
    Task<IReadOnlyList<VariableDeviceStateEntity>> GetByDeviceIdAsync(int deviceId,
        CancellationToken cancellationToken = default);
}
