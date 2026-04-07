using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IStandardVariableOverrideRepository : IRepository<StandardVariableOverrideEntity>
{
    /// <summary>
    /// Ottiene tutti gli override per un dizionario.
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverrideEntity>> GetByDictionaryIdAsync(int dictionaryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene l'override per una variabile standard in un dizionario specifico.
    /// </summary>
    Task<StandardVariableOverrideEntity?> GetByDictionaryAndVariableAsync(int dictionaryId,
        int standardVariableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti gli override per una variabile standard (tutti i dizionari).
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverrideEntity>> GetByVariableIdAsync(int standardVariableId,
        CancellationToken cancellationToken = default);
}
