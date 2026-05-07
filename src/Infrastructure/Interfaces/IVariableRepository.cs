using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IVariableRepository : IRepository<VariableEntity>
{
    Task<IReadOnlyList<VariableEntity>> GetByDictionaryIdAsync(int dictionaryId,
        CancellationToken cancellationToken = default);
    Task<VariableEntity?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se una variabile con l'Id specificato esiste.
    /// </summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene la variabile con le BitInterpretations incluse.
    /// </summary>
    Task<VariableEntity?> GetWithBitInterpretationsAsync(int id, CancellationToken cancellationToken = default);
}
