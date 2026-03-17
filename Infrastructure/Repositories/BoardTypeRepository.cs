using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BoardTypeRepository : RepositoryBase<BoardTypeEntity>, IBoardTypeRepository
{
    public BoardTypeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<BoardTypeEntity?> GetByNameAsync(string name, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(bt => bt.Name == name, cancellationToken);
    }

    public async Task<BoardTypeEntity?> GetByFirmwareTypeAsync(int firmwareType, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(bt => bt.FirmwareType == firmwareType, cancellationToken);
    }
}
