using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBitInterpretationRepository : IRepository<BitInterpretationEntity>
{
    /// <summary>
    /// Ottiene tutte le interpretazioni bit per una variabile.
    /// </summary>
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(int variableId, 
        CancellationToken cancellationToken = default);
}
