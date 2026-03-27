using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione schede.
/// Domain v2: nessun BoardType, Board ha FirmwareType diretto.
/// SESSION_035: DeviceType enum → DeviceId FK a Device entity.
/// </summary>
public interface IBoardService
{
    Task<Board?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default);
    Task<Board> AddAsync(Board board, CancellationToken ct = default);
    Task UpdateAsync(Board board, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Ottiene tutte le schede di un device.
    /// </summary>
    Task<IReadOnlyList<Board>> GetByDeviceIdAsync(int deviceId, CancellationToken ct = default);

    /// <summary>
    /// Cerca scheda per indirizzo protocol.
    /// </summary>
    Task<Board?> GetByProtocolAddressAsync(uint protocolAddress, CancellationToken ct = default);
}
