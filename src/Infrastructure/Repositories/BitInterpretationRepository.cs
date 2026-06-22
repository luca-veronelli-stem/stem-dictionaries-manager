using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class BitInterpretationRepository : RepositoryBase<BitInterpretationEntity>, IBitInterpretationRepository
{
    public BitInterpretationRepository(AppDbContext context, ILogger<RepositoryBase<BitInterpretationEntity>> logger)
        : base(context, logger)
    {
    }

    public async Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(int variableId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(bi => bi.VariableId == variableId)
            .OrderBy(bi => bi.DictionaryId)
            .ThenBy(bi => bi.WordIndex)
            .ThenBy(bi => bi.BitIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableAndDictionaryAsync(
        int variableId, int dictionaryId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(bi => bi.VariableId == variableId
                && (bi.DictionaryId == dictionaryId || bi.DictionaryId == null))
            .OrderBy(bi => bi.DictionaryId)
            .ThenBy(bi => bi.WordIndex)
            .ThenBy(bi => bi.BitIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task SyncByVariableIdAsync(int variableId, int? dictionaryId,
        IReadOnlyList<BitInterpretationEntity> incoming,
        CancellationToken cancellationToken = default)
    {
        List<BitInterpretationEntity> existing = await DbSet
            .Where(bi => bi.VariableId == variableId && bi.DictionaryId == dictionaryId)
            .ToListAsync(cancellationToken);

        var existingByKey = existing.ToDictionary(e => (e.WordIndex, e.BitIndex));
        var incomingByKey = incoming.ToDictionary(i => (i.WordIndex, i.BitIndex));

        // Delete: present in DB but not in the incoming list
        foreach (BitInterpretationEntity? e in existing)
        {
            if (!incomingByKey.ContainsKey((e.WordIndex, e.BitIndex)))
            {
                DbSet.Remove(e);
            }
        }

        // Add or Update
        foreach (BitInterpretationEntity i in incoming)
        {
            (int WordIndex, int BitIndex) key = (i.WordIndex, i.BitIndex);
            if (existingByKey.TryGetValue(key, out BitInterpretationEntity? e))
            {
                // Update only if the meaning has changed
                if (e.Meaning != i.Meaning)
                {
                    e.Meaning = i.Meaning;
                }
            }
            else
            {
                // Add
                await DbSet.AddAsync(new BitInterpretationEntity
                {
                    VariableId = variableId,
                    DictionaryId = dictionaryId,
                    WordIndex = i.WordIndex,
                    BitIndex = i.BitIndex,
                    Meaning = i.Meaning
                }, cancellationToken);
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
