using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class DeviceRepository : RepositoryBase<DeviceEntity>, IDeviceRepository
{
    public DeviceRepository(AppDbContext context, ILogger<RepositoryBase<DeviceEntity>> logger)
        : base(context, logger)
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
