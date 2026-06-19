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
        string normalizedName = name.ToLowerInvariant();
        return await DbSet
            .FirstOrDefaultAsync(d => d.Name.ToLower() == normalizedName, cancellationToken);
    }

    public async Task<DictionaryEntity?> GetWithVariablesAsync(int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<DictionaryEntity?> GetStandardDictionaryAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.IsStandard, cancellationToken);
    }

    public async Task<IReadOnlyList<DictionaryEntity>> GetAllWithVariablesAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Variables)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(d => d.Id == id, cancellationToken);
    }
}
