using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BitInterpretationRepository : RepositoryBase<BitInterpretationEntity>, IBitInterpretationRepository
{
    public BitInterpretationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(int variableId, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(bi => bi.VariableId == variableId)
            .OrderBy(bi => bi.WordIndex)
            .ThenBy(bi => bi.BitIndex)
            .ToListAsync(cancellationToken);
    }
}
