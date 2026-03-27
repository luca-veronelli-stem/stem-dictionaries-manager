using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DeviceRepository : RepositoryBase<DeviceEntity>, IDeviceRepository
{
    public DeviceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<DeviceEntity?> GetByNameAsync(string name,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(d => d.Name == name, cancellationToken);
    }

    public async Task<DeviceEntity?> GetByMachineCodeAsync(int machineCode,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(d => d.MachineCode == machineCode, cancellationToken);
    }
}
