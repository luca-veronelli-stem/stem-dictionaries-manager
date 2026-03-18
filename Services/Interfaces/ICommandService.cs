using Core.Enums;
using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione comandi protocollo.
/// </summary>
public interface ICommandService
{
    // === CRUD Base ===
    
    Task<Command?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default);
    Task<Command> AddAsync(Command command, CancellationToken ct = default);
    Task UpdateAsync(Command command, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    
    // === Query Specifiche ===
    
    /// <summary>
    /// Cerca comando per codice e tipo (request/response).
    /// </summary>
    Task<Command?> GetByCodeAsync(byte codeHigh, byte codeLow, bool isResponse, 
        CancellationToken ct = default);
    
    // === DeviceState Management ===
    
    /// <summary>
    /// Ottiene comando con tutti gli stati device caricati.
    /// </summary>
    Task<Command?> GetWithDeviceStatesAsync(int id, CancellationToken ct = default);
    
    /// <summary>
    /// Imposta lo stato abilitato/disabilitato di un comando per un device.
    /// </summary>
    Task SetDeviceStateAsync(int commandId, DeviceType deviceType, bool isEnabled, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Ottiene lo stato di un comando per un device specifico.
    /// </summary>
    Task<CommandDeviceState?> GetDeviceStateAsync(int commandId, DeviceType deviceType, 
        CancellationToken ct = default);
}
