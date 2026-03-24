using Core.Enums;
using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione schede.
/// Domain v2: nessun BoardType, Board ha FirmwareType diretto.
/// </summary>
public interface IBoardService
{
    Task<Board?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default);
    Task<Board> AddAsync(Board board, CancellationToken ct = default);
    Task UpdateAsync(Board board, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Ottiene tutte le schede di un tipo device.
    /// </summary>
    Task<IReadOnlyList<Board>> GetByDeviceTypeAsync(DeviceType deviceType, CancellationToken ct = default);

    /// <summary>
    /// Cerca scheda per indirizzo protocol.
    /// </summary>
    Task<Board?> GetByProtocolAddressAsync(uint protocolAddress, CancellationToken ct = default);
}
