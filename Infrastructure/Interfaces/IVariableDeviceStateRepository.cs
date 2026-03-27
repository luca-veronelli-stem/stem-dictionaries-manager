using Core.Enums;
using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IVariableDeviceStateRepository : IRepository<VariableDeviceStateEntity>
{
    /// <summary>
    /// Ottiene lo stato di una variabile per un tipo di device specifico.
    /// </summary>
    Task<VariableDeviceStateEntity?> GetByVariableAndDeviceAsync(int variableId, DeviceType deviceType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti gli stati per una variabile.
    /// </summary>
    Task<IReadOnlyList<VariableDeviceStateEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti gli stati per un tipo di device.
    /// </summary>
    Task<IReadOnlyList<VariableDeviceStateEntity>> GetByDeviceTypeAsync(DeviceType deviceType,
        CancellationToken cancellationToken = default);
}
