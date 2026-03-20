using Core.Enums;
using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IDictionaryRepository : IRepository<DictionaryEntity>
{
    Task<DictionaryEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetByBoardTypeAsync(int boardTypeId, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetWithVariablesAsync(int id, CancellationToken cancellationToken = default);
    Task<DictionaryEntity?> GetStandardDictionaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cerca un dizionario per combinazione (DeviceType, BoardTypeId).
    /// </summary>
    Task<DictionaryEntity?> GetByDeviceTypeAndBoardTypeAsync(DeviceType deviceType, int boardTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene tutti i dizionari con BoardType incluso.
    /// </summary>
    Task<IReadOnlyList<DictionaryEntity>> GetAllWithBoardTypeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se un dizionario con l'Id specificato esiste.
    /// </summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}
