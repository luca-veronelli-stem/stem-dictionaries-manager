using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;

namespace Tests.Unit.Services.Auth.Fakes;

/// <summary>
/// Manual fake for <see cref="IBootstrapTokenRepository"/>. Mirrors the
/// EF-backed semantics: <see cref="AddAsync"/>/<see cref="UpdateAsync"/>
/// only track changes (no implicit save), and a synthetic auto-increment
/// id is assigned on add so domain code can capture it for downstream
/// FK references.
/// </summary>
internal sealed class FakeBootstrapTokenRepository : IBootstrapTokenRepository
{
    private readonly List<BootstrapTokenEntity> _store = [];
    private int _nextId = 1;

    public int UpdateCallCount { get; private set; }

    public BootstrapTokenEntity Seed(BootstrapTokenEntity entity)
    {
        if (entity.Id == 0)
        {
            entity.Id = _nextId++;
        }
        else if (entity.Id >= _nextId)
        {
            _nextId = entity.Id + 1;
        }
        _store.Add(entity);
        return entity;
    }

    public Task<BootstrapTokenEntity?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_store.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<BootstrapTokenEntity>> ListByStatusAsync(
        BootstrapTokenStatus status, CancellationToken ct = default)
    {
        IReadOnlyList<BootstrapTokenEntity> matched = _store
            .Where(t => t.Status == status)
            .ToList();
        return Task.FromResult(matched);
    }

    public Task<BootstrapTokenEntity> AddAsync(BootstrapTokenEntity entity,
        CancellationToken ct = default)
    {
        Seed(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(BootstrapTokenEntity entity, CancellationToken ct = default)
    {
        UpdateCallCount++;
        return Task.CompletedTask;
    }
}
