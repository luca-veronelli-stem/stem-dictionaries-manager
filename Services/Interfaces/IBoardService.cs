using Core.Enums;
using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione schede e tipi scheda.
/// </summary>
public interface IBoardService
{
    // === Board CRUD ===
    
    Task<Board?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default);
    Task<Board> AddAsync(Board board, CancellationToken ct = default);
    Task UpdateAsync(Board board, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    
    // === Board Query ===
    
    /// <summary>
    /// Ottiene tutte le schede di un tipo device.
    /// </summary>
    Task<IReadOnlyList<Board>> GetByDeviceTypeAsync(DeviceType deviceType, CancellationToken ct = default);
    
    /// <summary>
    /// Cerca scheda per indirizzo protocol.
    /// </summary>
    Task<Board?> GetByProtocolAddressAsync(uint protocolAddress, CancellationToken ct = default);
    
    // === BoardType Operations ===
    
    /// <summary>
    /// Ottiene tutti i tipi scheda.
    /// </summary>
    Task<IReadOnlyList<BoardType>> GetBoardTypesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Cerca tipo scheda per nome.
    /// </summary>
    Task<BoardType?> GetBoardTypeByNameAsync(string name, CancellationToken ct = default);
    
    /// <summary>
    /// Cerca tipo scheda per firmware type.
    /// </summary>
    Task<BoardType?> GetBoardTypeByFirmwareTypeAsync(int firmwareType, CancellationToken ct = default);
    
    /// <summary>
    /// Aggiunge un nuovo tipo scheda.
    /// </summary>
    Task<BoardType> AddBoardTypeAsync(BoardType boardType, CancellationToken ct = default);
}
