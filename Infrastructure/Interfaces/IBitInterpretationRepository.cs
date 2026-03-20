using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBitInterpretationRepository : IRepository<BitInterpretationEntity>
{
    /// <summary>
    /// Ottiene tutte le interpretazioni bit per una variabile.
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sincronizza le interpretazioni bit di una variabile con la lista incoming.
    /// Confronta per chiave naturale (WordIndex, BitIndex):
    /// - Aggiunge le nuove, aggiorna le modificate, elimina le rimosse.
    /// Operazione atomica (singolo SaveChanges).
    /// </summary>
    Task SyncByVariableIdAsync(int variableId,
        IReadOnlyList<BitInterpretationEntity> incoming,
        CancellationToken cancellationToken = default);
}
