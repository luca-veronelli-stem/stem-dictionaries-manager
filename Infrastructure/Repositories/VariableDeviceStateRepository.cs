using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VariableDeviceStateRepository : RepositoryBase<VariableDeviceStateEntity>, IVariableDeviceStateRepository
{
    public VariableDeviceStateRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<VariableDeviceStateEntity?> GetByVariableAndDeviceAsync(int variableId, int deviceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.VariableId == variableId && s.DeviceId == deviceId, cancellationToken);
    }

    public async Task<IReadOnlyList<VariableDeviceStateEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.VariableId == variableId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VariableDeviceStateEntity>> GetByDeviceIdAsync(int deviceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.DeviceId == deviceId)
            .ToListAsync(cancellationToken);
    }
}
