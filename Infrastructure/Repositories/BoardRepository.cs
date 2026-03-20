using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BoardRepository : RepositoryBase<BoardEntity>, IBoardRepository
{
    public BoardRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<IReadOnlyList<BoardEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.BoardType)
            .ToListAsync(cancellationToken);
    }

    public override async Task<BoardEntity?> GetByIdAsync(int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.BoardType)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BoardEntity>> GetByDeviceTypeAsync(DeviceType deviceType,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.BoardType)
            .Where(b => b.DeviceType == deviceType)
            .ToListAsync(cancellationToken);
    }

    public async Task<BoardEntity?> GetByProtocolAddressAsync(uint protocolAddress,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.BoardType)
            .FirstOrDefaultAsync(b => b.ProtocolAddress == protocolAddress, cancellationToken);
    }
}
