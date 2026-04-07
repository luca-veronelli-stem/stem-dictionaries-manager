using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBitInterpretationRepository : IRepository<BitInterpretationEntity>
{
    /// <summary>
    /// Ottiene tutte le interpretazioni bit per una variabile (tutti i dizionari, incluse template).
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene le interpretazioni bit per una variabile e un dizionario specifico.
    /// Ritorna sia le interpretazioni con DictionaryId=dictionaryId sia quelle con DictionaryId=null (template).
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableAndDictionaryAsync(int variableId,
        int dictionaryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sincronizza le interpretazioni bit di una variabile per un dato dictionaryId (o null per template).
    /// Confronta per chiave naturale (WordIndex, BitIndex) filtrando per DictionaryId:
    /// - Aggiunge le nuove, aggiorna le modificate, elimina le rimosse.
    /// Operazione atomica (singolo SaveChanges).
    /// </summary>
    Task SyncByVariableIdAsync(int variableId, int? dictionaryId,
        IReadOnlyList<BitInterpretationEntity> incoming,
        CancellationToken cancellationToken = default);
}
