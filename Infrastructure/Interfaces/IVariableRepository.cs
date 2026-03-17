using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IVariableRepository : IRepository<VariableEntity>
{
    Task<IReadOnlyList<VariableEntity>> GetByDictionaryIdAsync(int dictionaryId, 
        CancellationToken cancellationToken = default);
    Task<VariableEntity?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow, 
        CancellationToken cancellationToken = default);
}
