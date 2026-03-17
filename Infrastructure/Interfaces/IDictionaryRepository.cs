using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IDictionaryRepository : IRepository<DictionaryEntity>
{
    Task<DictionaryEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetWithVariablesAsync(int id, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetStandardDictionaryAsync(CancellationToken cancellationToken = default);
}
