using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IVariableRepository : IRepository<VariableEntity>
{
    Task<IReadOnlyList<VariableEntity>> GetByDictionaryIdAsync(int dictionaryId,
        CancellationToken cancellationToken = default);
    Task<VariableEntity?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a variable with the given Id exists.
    /// </summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the variable with BitInterpretations included.
    /// </summary>
    Task<VariableEntity?> GetWithBitInterpretationsAsync(int id, CancellationToken cancellationToken = default);
}
