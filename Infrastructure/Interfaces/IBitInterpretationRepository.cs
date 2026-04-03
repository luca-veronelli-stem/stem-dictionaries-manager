using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBitInterpretationRepository : IRepository<BitInterpretationEntity>
{
    /// <summary>
    /// Ottiene tutte le interpretazioni bit per una variabile (tutte le device, incluse comuni).
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene le interpretazioni bit per una variabile e un device specifico.
    /// Ritorna sia le interpretazioni con DeviceId=deviceId sia quelle con DeviceId=null (comuni).
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableAndDeviceAsync(int variableId,
        int deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sincronizza le interpretazioni bit di una variabile per un dato deviceId (o null per comuni).
    /// Confronta per chiave naturale (WordIndex, BitIndex) filtrando per DeviceId:
    /// - Aggiunge le nuove, aggiorna le modificate, elimina le rimosse.
    /// Operazione atomica (singolo SaveChanges).
    /// </summary>
    Task SyncByVariableIdAsync(int variableId, int? deviceId,
        IReadOnlyList<BitInterpretationEntity> incoming,
        CancellationToken cancellationToken = default);
}
