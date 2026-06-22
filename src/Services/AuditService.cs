using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Audit trail service implementation.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IAuditEntryRepository _repository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditEntryRepository repository, ILogger<AuditService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        _repository = repository;
        _logger = logger;
    }

    // === Query ===

    public async Task<AuditEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        AuditEntryEntity? entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : AuditEntryMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(
        AuditEntityType entityType, int entityId, CancellationToken ct = default)
    {
        IReadOnlyList<AuditEntryEntity> entities = await _repository.GetByEntityAsync(entityType, entityId, ct);
        return AuditEntryMapper.ToDomainList(entities);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetByUserAsync(
        int userId, CancellationToken ct = default)
    {
        IReadOnlyList<AuditEntryEntity> entities = await _repository.GetByUserAsync(userId, ct);
        return AuditEntryMapper.ToDomainList(entities);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetRecentAsync(
        int count = 100, CancellationToken ct = default)
    {
        IReadOnlyList<AuditEntryEntity> entities = await _repository.GetRecentAsync(count, ct);
        return AuditEntryMapper.ToDomainList(entities);
    }

    public async Task<IReadOnlyList<AuditEntry>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (from > to)
        {
            throw new ArgumentException(
                $"'from' ({from:O}) must be before 'to' ({to:O}).");
        }

        IReadOnlyList<AuditEntryEntity> entities = await _repository.GetByDateRangeAsync(from, to, ct);
        return AuditEntryMapper.ToDomainList(entities);
    }

    // === Log ===

    public async Task LogCreateAsync(AuditEntityType entityType, int entityId,
        int changedById, string newValueJson, string? notes = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newValueJson);

        var entry = AuditEntry.ForCreate(
            entityType, entityId, changedById, newValueJson, notes);
        AuditEntryEntity entity = AuditEntryMapper.ToEntity(entry);
        await _repository.AddAsync(entity, ct);

        _logger.LogDebug(
            "Recorded create audit entry for {EntityType} {EntityId} by user {UserId}",
            entityType, entityId, changedById);
    }

    public async Task LogUpdateAsync(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string newValueJson,
        string? notes = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previousValueJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(newValueJson);

        var entry = AuditEntry.ForUpdate(
            entityType, entityId, changedById,
            previousValueJson, newValueJson, notes);
        AuditEntryEntity entity = AuditEntryMapper.ToEntity(entry);
        await _repository.AddAsync(entity, ct);
    }

    public async Task LogDeleteAsync(AuditEntityType entityType, int entityId,
        int changedById, string previousValueJson, string? notes = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previousValueJson);

        var entry = AuditEntry.ForDelete(
            entityType, entityId, changedById, previousValueJson, notes);
        AuditEntryEntity entity = AuditEntryMapper.ToEntity(entry);
        await _repository.AddAsync(entity, ct);
    }
}
