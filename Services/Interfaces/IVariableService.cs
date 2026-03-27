using Core.Enums;
using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione variabili singole.
/// Per operazioni batch, usare IDictionaryService.
/// </summary>
public interface IVariableService
{
    // === CRUD Base ===

    Task<Variable?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Variable>> GetAllAsync(CancellationToken ct = default);
    Task<Variable> AddAsync(int dictionaryId, Variable variable, CancellationToken ct = default);
    Task UpdateAsync(Variable variable, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    // === Query Specifiche ===

    /// <summary>
    /// Ottiene tutte le variabili di un dizionario.
    /// </summary>
    Task<IReadOnlyList<Variable>> GetByDictionaryIdAsync(int dictionaryId, CancellationToken ct = default);

    /// <summary>
    /// Cerca variabile per indirizzo in un dizionario.
    /// </summary>
    Task<Variable?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow,
        CancellationToken ct = default);

    // === BitInterpretation Management ===

    /// <summary>
    /// Ottiene le interpretazioni bit per una variabile bitmapped.
    /// </summary>
    Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsAsync(int variableId,
        CancellationToken ct = default);

    /// <summary>
    /// Aggiunge un'interpretazione bit a una variabile bitmapped.
    /// </summary>
    Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation,
        CancellationToken ct = default);

    /// <summary>
    /// Sincronizza le interpretazioni bit di una variabile bitmapped.
    /// Smart update: confronta per (WordIndex, BitIndex), aggiunge/aggiorna/elimina.
    /// </summary>
    Task UpdateBitInterpretationsAsync(int variableId, IEnumerable<BitInterpretation> interpretations,
        CancellationToken ct = default);

    // === DeviceState Management ===

    /// <summary>
    /// Imposta lo stato abilitato/disabilitato di una variabile per un device.
    /// BR-011: se Variable.IsEnabled=false, isEnabled=true è vietato.
    /// </summary>
    Task SetDeviceStateAsync(int variableId, DeviceType deviceType, bool isEnabled,
        CancellationToken ct = default);

    /// <summary>
    /// Ottiene lo stato di una variabile per un device specifico.
    /// </summary>
    Task<VariableDeviceState?> GetDeviceStateAsync(int variableId, DeviceType deviceType,
        CancellationToken ct = default);

    /// <summary>
    /// Ottiene tutti gli override per-device di una variabile.
    /// </summary>
    Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesAsync(int variableId,
        CancellationToken ct = default);

    /// <summary>
    /// Ottiene tutti gli override per un tipo di device.
    /// </summary>
    Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesForDeviceAsync(
        DeviceType deviceType, CancellationToken ct = default);
}
