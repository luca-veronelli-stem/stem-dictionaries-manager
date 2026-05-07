using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IStandardVariableOverrideRepository : IRepository<StandardVariableOverrideEntity>
{
    /// <summary>
    /// Gets all overrides for a dictionary.
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverrideEntity>> GetByDictionaryIdAsync(int dictionaryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the override for a standard variable in a specific dictionary.
    /// </summary>
    Task<StandardVariableOverrideEntity?> GetByDictionaryAndVariableAsync(int dictionaryId,
        int standardVariableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all overrides for a standard variable (across all dictionaries).
    /// </summary>
    Task<IReadOnlyList<StandardVariableOverrideEntity>> GetByVariableIdAsync(int standardVariableId,
        CancellationToken cancellationToken = default);
}
