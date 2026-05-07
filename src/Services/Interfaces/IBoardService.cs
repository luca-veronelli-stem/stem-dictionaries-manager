using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Board service.
/// Domain v2: no BoardType, Board has FirmwareType directly.
/// SESSION_035: DeviceType enum → DeviceId FK to Device entity.
/// </summary>
public interface IBoardService
{
    Task<Board?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default);
    Task<Board> AddAsync(Board board, CancellationToken ct = default);
    Task UpdateAsync(Board board, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Gets all boards of a device.
    /// </summary>
    Task<IReadOnlyList<Board>> GetByDeviceIdAsync(int deviceId, CancellationToken ct = default);

    /// <summary>
    /// Looks up a board by protocol address.
    /// </summary>
    Task<Board?> GetByProtocolAddressAsync(uint protocolAddress, CancellationToken ct = default);

    /// <summary>
    /// Computes the first available FirmwareType (global max + 1).
    /// </summary>
    Task<int> GetNextAvailableFirmwareTypeAsync(CancellationToken ct = default);
}
