using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Auth;

/// <summary>
/// EF Core repository for <see cref="RegistrationEventEntity"/>.
/// Append-only audit table — no Update/Delete on the surface.
/// Add tracks the change; caller commits via the shared
/// <see cref="AppDbContext"/> for atomic multi-entity writes.
/// </summary>
public class RegistrationEventRepository : IRegistrationEventRepository
{
    private readonly AppDbContext _context;

    public RegistrationEventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<RegistrationEventEntity> AddAsync(RegistrationEventEntity entity,
        CancellationToken ct = default)
    {
        await _context.RegistrationEvents.AddAsync(entity, ct);
        return entity;
    }

    public async Task<IReadOnlyList<RegistrationEventEntity>> ListBySourceAsync(string sourceIp,
        DateTime since, CancellationToken ct = default)
    {
        return await _context.RegistrationEvents
            .Where(e => e.SourceIp == sourceIp && e.OccurredAt >= since)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct);
    }
}
