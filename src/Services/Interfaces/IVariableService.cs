using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Single-variable service.
/// For batch operations, use IDictionaryService.
/// v7: DeviceState → StandardVariableOverride (per-dictionary).
/// </summary>
public interface IVariableService
{
    // === Base CRUD ===

    Task<Variable?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Variable>> GetAllAsync(CancellationToken ct = default);
    Task<Variable> AddAsync(int dictionaryId, Variable variable, CancellationToken ct = default);
    Task UpdateAsync(Variable variable, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    // === Specific queries ===

    Task<IReadOnlyList<Variable>> GetByDictionaryIdAsync(int dictionaryId, CancellationToken ct = default);

    Task<Variable?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow,
        CancellationToken ct = default);

    // === BitInterpretation management ===

    Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsAsync(int variableId,
        CancellationToken ct = default);

    Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation,
        CancellationToken ct = default);

    Task UpdateBitInterpretationsAsync(int variableId, IEnumerable<BitInterpretation> interpretations,
        CancellationToken ct = default);

    /// <summary>
    /// Updates bit interpretations for a variable and a specific dictionary (or null for the template).
    /// </summary>
    Task UpdateBitInterpretationsForDictionaryAsync(int variableId, int? dictionaryId,
        IEnumerable<BitInterpretation> interpretations, CancellationToken ct = default);

    /// <summary>
    /// Gets bit interpretations for a variable and a dictionary (falls back to the template).
    /// </summary>
    Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsForDictionaryAsync(int variableId,
        int dictionaryId, CancellationToken ct = default);

    // === StandardVariableOverride management ===

    /// <summary>
    /// Sets or updates the override of a standard variable for a dictionary.
    /// BR-011: isEnabled=true is forbidden when Variable.IsEnabled=false.
    /// </summary>
    Task SetOverrideAsync(int dictionaryId, int standardVariableId, bool isEnabled,
        string? description = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the override of a standard variable for a specific dictionary.
    /// </summary>
    Task<StandardVariableOverride?> GetOverrideAsync(int dictionaryId, int standardVariableId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all overrides for a dictionary.
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverride>> GetOverridesByDictionaryAsync(int dictionaryId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all overrides for a standard variable (across all dictionaries).
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverride>> GetOverridesByVariableAsync(
        int standardVariableId, CancellationToken ct = default);
}
