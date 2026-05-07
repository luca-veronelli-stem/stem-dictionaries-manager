using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IDictionaryRepository : IRepository<DictionaryEntity>
{
    Task<DictionaryEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetWithVariablesAsync(int id, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetStandardDictionaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti i dizionari con le variabili incluse.
    /// </summary>
    Task<IReadOnlyList<DictionaryEntity>> GetAllWithVariablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se un dizionario con l'Id specificato esiste.
    /// </summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
