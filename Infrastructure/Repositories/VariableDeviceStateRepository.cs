using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VariableDeviceStateRepository : RepositoryBase<VariableDeviceStateEntity>, IVariableDeviceStateRepository
{
    public VariableDeviceStateRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<VariableDeviceStateEntity?> GetByVariableAndDeviceAsync(int variableId, DeviceType deviceType,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(s => s.VariableId == variableId && s.DeviceType == deviceType, cancellationToken);
    }

    public async Task<IReadOnlyList<VariableDeviceStateEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.VariableId == variableId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VariableDeviceStateEntity>> GetByDeviceTypeAsync(DeviceType deviceType,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(s => s.DeviceType == deviceType)
            .ToListAsync(cancellationToken);
    }
}
