using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class BoardRepository : RepositoryBase<BoardEntity>, IBoardRepository
{
    public BoardRepository(AppDbContext context, ILogger<RepositoryBase<BoardEntity>> logger)
        : base(context, logger)
    {
    }

    public override async Task<IReadOnlyList<BoardEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.Dictionary)
            .Include(b => b.Device)
            .ToListAsync(cancellationToken);
    }

    public override async Task<BoardEntity?> GetByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.Dictionary)
            .Include(b => b.Device)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BoardEntity>> GetByDeviceIdAsync(int deviceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.Dictionary)
            .Include(b => b.Device)
            .Where(b => b.DeviceId == deviceId)
            .ToListAsync(cancellationToken);
    }

    public async Task<BoardEntity?> GetByProtocolAddressAsync(uint protocolAddress,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.Dictionary)
            .FirstOrDefaultAsync(b => b.ProtocolAddress == protocolAddress, cancellationToken);
    }
}
