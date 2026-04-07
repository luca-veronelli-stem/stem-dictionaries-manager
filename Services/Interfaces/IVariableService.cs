using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione variabili singole.
/// Per operazioni batch, usare IDictionaryService.
/// v7: DeviceState → StandardVariableOverride (per-dizionario).
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

    Task<IReadOnlyList<Variable>> GetByDictionaryIdAsync(int dictionaryId, CancellationToken ct = default);

    Task<Variable?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow,
        CancellationToken ct = default);

    // === BitInterpretation Management ===

    Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsAsync(int variableId,
        CancellationToken ct = default);

    Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation,
        CancellationToken ct = default);

    Task UpdateBitInterpretationsAsync(int variableId, IEnumerable<BitInterpretation> interpretations,
        CancellationToken ct = default);

    /// <summary>
    /// Aggiorna le interpretazioni bit per una variabile e un dizionario specifico (o null per template).
    /// </summary>
    Task UpdateBitInterpretationsForDictionaryAsync(int variableId, int? dictionaryId,
        IEnumerable<BitInterpretation> interpretations, CancellationToken ct = default);

    /// <summary>
    /// Ottiene le interpretazioni bit per una variabile e un dizionario (include template come fallback).
    /// </summary>
    Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsForDictionaryAsync(int variableId,
        int dictionaryId, CancellationToken ct = default);

    // === StandardVariableOverride Management ===

    /// <summary>
    /// Imposta o aggiorna l'override di una variabile standard per un dizionario.
    /// BR-011: isEnabled=true vietato se Variable.IsEnabled=false.
    /// </summary>
    Task SetOverrideAsync(int dictionaryId, int standardVariableId, bool isEnabled,
        string? description = null, CancellationToken ct = default);

    /// <summary>
    /// Ottiene l'override di una variabile standard per un dizionario specifico.
    /// </summary>
    Task<StandardVariableOverride?> GetOverrideAsync(int dictionaryId, int standardVariableId,
        CancellationToken ct = default);

    /// <summary>
    /// Ottiene tutti gli override per un dizionario.
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverride>> GetOverridesByDictionaryAsync(int dictionaryId,
        CancellationToken ct = default);

    /// <summary>
    /// Ottiene tutti gli override per una variabile standard (tutti i dizionari).
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverride>> GetOverridesByVariableAsync(
        int standardVariableId, CancellationToken ct = default);
}
