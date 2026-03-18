using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione dizionari (aggregate root).
/// Include operazioni su Variables tramite Dictionary.
/// </summary>
public interface IDictionaryService
{
    // === CRUD Base ===
    
    Task<Dictionary?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default);
    Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default);
    Task UpdateAsync(Dictionary dictionary, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    
    // === Query Specifiche ===
    
    /// <summary>
    /// Cerca dizionario per nome (case-insensitive).
    /// </summary>
    Task<Dictionary?> GetByNameAsync(string name, CancellationToken ct = default);
    
    /// <summary>
    /// Ottiene il dizionario associato a un BoardType.
    /// </summary>
    Task<Dictionary?> GetByBoardTypeIdAsync(int boardTypeId, CancellationToken ct = default);
    
    /// <summary>
    /// Ottiene il dizionario "Standard" (senza BoardType).
    /// </summary>
    Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default);
    
    // === Aggregate Operations ===
    
    /// <summary>
    /// Ottiene dizionario con tutte le variabili caricate.
    /// </summary>
    Task<Dictionary?> GetWithVariablesAsync(int id, CancellationToken ct = default);
    
    /// <summary>
    /// Aggiunge una variabile al dizionario.
    /// Valida unicità indirizzo.
    /// </summary>
    Task<Variable> AddVariableAsync(int dictionaryId, Variable variable, CancellationToken ct = default);
    
    /// <summary>
    /// Rimuove una variabile dal dizionario.
    /// </summary>
    Task RemoveVariableAsync(int dictionaryId, int variableId, CancellationToken ct = default);
}
