using Core.Enums;
using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione audit trail.
/// Query: lettura cronologia modifiche.
/// Log: registrazione modifiche (chiamato dagli altri service).
/// </summary>
public interface IAuditService
{
    // === Query ===

    /// <summary>
    /// Recupera un'entry di audit per ID.
    /// </summary>
    Task<AuditEntry?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Recupera le entry di audit per una specifica entità.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        AuditEntityType entityType, int entityId,
        CancellationToken ct = default);

    /// <summary>
    /// Recupera le entry di audit di un utente.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByUserAsync(
        int userId, CancellationToken ct = default);

    /// <summary>
    /// Recupera le entry di audit più recenti.
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetRecentAsync(
        int count = 100, CancellationToken ct = default);

    /// <summary>
    /// Recupera le entry di audit in un intervallo di date (UTC).
    /// </summary>
    Task<IReadOnlyList<AuditEntry>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default);

    // === Log ===

    /// <summary>
    /// Registra un'operazione di creazione.
    /// </summary>
    Task LogCreateAsync(AuditEntityType entityType, int entityId,
        int changedById, string newValueJson, string? notes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Registra un'operazione di aggiornamento.
    /// </summary>
    Task LogUpdateAsync(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string newValueJson,
        string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Registra un'operazione di eliminazione.
    /// </summary>
    Task LogDeleteAsync(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string? notes = null,
        CancellationToken ct = default);
}
