using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IDictionaryRepository : IRepository<DictionaryEntity>
{
    Task<DictionaryEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetWithVariablesAsync(int id, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetStandardDictionaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all dictionaries with variables included.
    /// </summary>
    Task<IReadOnlyList<DictionaryEntity>> GetAllWithVariablesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a dictionary with the given Id exists.
    /// </summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
