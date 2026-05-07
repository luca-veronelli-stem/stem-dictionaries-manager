using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Protocol command service.
/// SESSION_035: DeviceType enum → int deviceId.
/// </summary>
public interface ICommandService
{
    // === Base CRUD ===

    Task<Command?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default);
    Task<Command> AddAsync(Command command, CancellationToken ct = default);
    Task UpdateAsync(Command command, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    // === Specific queries ===

    Task<Command?> GetByCodeAsync(byte codeHigh, byte codeLow, bool isResponse,
        CancellationToken ct = default);

    // === DeviceState Management ===

    Task<Command?> GetWithDeviceStatesAsync(int id, CancellationToken ct = default);

    Task SetDeviceStateAsync(int commandId, int deviceId, bool isEnabled,
        CancellationToken ct = default);

    Task<CommandDeviceState?> GetDeviceStateAsync(int commandId, int deviceId,
        CancellationToken ct = default);

    Task<IReadOnlyList<CommandDeviceState>> GetDeviceStatesForDeviceAsync(
        int deviceId, CancellationToken ct = default);
}
