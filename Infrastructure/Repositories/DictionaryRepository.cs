using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class DictionaryRepository : RepositoryBase<DictionaryEntity>, IDictionaryRepository
{
    public DictionaryRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<DictionaryEntity?> GetByNameAsync(string name, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(d => d.Name == name, cancellationToken);
    }

    public async Task<DictionaryEntity?> GetByBoardTypeAsync(int boardTypeId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.BoardType)
            .FirstOrDefaultAsync(d => d.BoardTypeId == boardTypeId, cancellationToken);
    }

    public async Task<DictionaryEntity?> GetWithVariablesAsync(int id, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Variables)
            .Include(d => d.BoardType)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DictionaryEntity?> GetStandardDictionaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.BoardTypeId == null && d.DeviceType == null, cancellationToken);
    }

    public async Task<DictionaryEntity?> GetByDeviceTypeAndBoardTypeAsync(DeviceType deviceType, int boardTypeId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.BoardType)
            .FirstOrDefaultAsync(d => d.DeviceType == deviceType && d.BoardTypeId == boardTypeId, cancellationToken);
    }

    public async Task<IReadOnlyList<DictionaryEntity>> GetAllWithBoardTypeAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.BoardType)
            .Include(d => d.Variables)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(d => d.Id == id, cancellationToken);
    }
}
