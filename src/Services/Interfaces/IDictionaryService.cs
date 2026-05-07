using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione dizionari (aggregate root).
/// Domain v2: IsStandard flag, nessun BoardType/DeviceType.
/// </summary>
public interface IDictionaryService
{
    Task<Dictionary?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default);
    Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default);
    Task UpdateAsync(Dictionary dictionary, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<Dictionary?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Ottiene il dizionario Standard (IsStandard=true, max 1).
    /// </summary>
    Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default);

    Task<Dictionary?> GetWithVariablesAsync(int id, CancellationToken ct = default);
    Task<Variable> AddVariableAsync(int dictionaryId, Variable variable, CancellationToken ct = default);
    Task RemoveVariableAsync(int dictionaryId, int variableId, CancellationToken ct = default);
}
