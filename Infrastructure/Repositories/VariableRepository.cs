using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class VariableRepository : RepositoryBase<VariableEntity>, IVariableRepository
{
    public VariableRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<VariableEntity>> GetByDictionaryIdAsync(int dictionaryId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(v => v.DictionaryId == dictionaryId)
            .OrderBy(v => v.AddressHigh)
            .ThenBy(v => v.AddressLow)
            .ToListAsync(cancellationToken);
    }

    public async Task<VariableEntity?> GetByAddressAsync(int dictionaryId, byte addressHigh, 
        byte addressLow, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(v => 
                v.DictionaryId == dictionaryId && 
                v.AddressHigh == addressHigh && 
                v.AddressLow == addressLow, 
                cancellationToken);
    }
}
