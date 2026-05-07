using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CommandDeviceStateRepository : RepositoryBase<CommandDeviceStateEntity>, ICommandDeviceStateRepository
{
    public CommandDeviceStateRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CommandDeviceStateEntity?> GetByCommandAndDeviceAsync(int commandId, int deviceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.CommandId == commandId && s.DeviceId == deviceId, cancellationToken);
    }

    public async Task<IReadOnlyList<CommandDeviceStateEntity>> GetByCommandIdAsync(int commandId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.CommandId == commandId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommandDeviceStateEntity>> GetByDeviceIdAsync(int deviceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.DeviceId == deviceId)
            .ToListAsync(cancellationToken);
    }
}
