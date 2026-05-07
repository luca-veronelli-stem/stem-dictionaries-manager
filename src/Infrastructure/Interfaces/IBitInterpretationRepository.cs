using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBitInterpretationRepository : IRepository<BitInterpretationEntity>
{
    /// <summary>
    /// Gets all bit interpretations for a variable (all dictionaries, including templates).
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bit interpretations for a variable in a specific dictionary.
    /// Returns both entries with DictionaryId=dictionaryId and those with DictionaryId=null (template).
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableAndDictionaryAsync(int variableId,
        int dictionaryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronizes a variable's bit interpretations for a given dictionaryId (or null for template).
    /// Matches by natural key (WordIndex, BitIndex) filtering by DictionaryId:
    /// - Adds new ones, updates changed ones, removes deleted ones.
    /// Atomic operation (single SaveChanges).
    /// </summary>
    Task SyncByVariableIdAsync(int variableId, int? dictionaryId,
        IReadOnlyList<BitInterpretationEntity> incoming,
        CancellationToken cancellationToken = default);
}
