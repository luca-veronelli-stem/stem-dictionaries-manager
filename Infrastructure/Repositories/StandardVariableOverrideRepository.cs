using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class StandardVariableOverrideRepository
    : RepositoryBase<StandardVariableOverrideEntity>, IStandardVariableOverrideRepository
{
    public StandardVariableOverrideRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<StandardVariableOverrideEntity>> GetByDictionaryIdAsync(
        int dictionaryId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.DictionaryId == dictionaryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<StandardVariableOverrideEntity?> GetByDictionaryAndVariableAsync(
        int dictionaryId, int standardVariableId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(o => o.DictionaryId == dictionaryId
                && o.StandardVariableId == standardVariableId, cancellationToken);
    }

    public async Task<IReadOnlyList<StandardVariableOverrideEntity>> GetByVariableIdAsync(
        int standardVariableId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(o => o.StandardVariableId == standardVariableId)
            .ToListAsync(cancellationToken);
    }
}
