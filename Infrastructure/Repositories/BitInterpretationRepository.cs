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
            .OrderBy(bi => bi.DeviceId)
            .ThenBy(bi => bi.WordIndex)
            .ThenBy(bi => bi.BitIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableAndDeviceAsync(int variableId,
        int deviceId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(bi => bi.VariableId == variableId
                && (bi.DeviceId == deviceId || bi.DeviceId == null))
            .OrderBy(bi => bi.DeviceId)
            .ThenBy(bi => bi.WordIndex)
            .ThenBy(bi => bi.BitIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task SyncByVariableIdAsync(int variableId, int? deviceId,
        IReadOnlyList<BitInterpretationEntity> incoming,
        CancellationToken cancellationToken = default)
    {
        var existing = await DbSet
            .Where(bi => bi.VariableId == variableId && bi.DeviceId == deviceId)
            .ToListAsync(cancellationToken);

        var existingByKey = existing.ToDictionary(e => (e.WordIndex, e.BitIndex));
        var incomingByKey = incoming.ToDictionary(i => (i.WordIndex, i.BitIndex));

        // Delete: nel DB ma non nella lista incoming
        foreach (var e in existing)
        {
            if (!incomingByKey.ContainsKey((e.WordIndex, e.BitIndex)))
                DbSet.Remove(e);
        }

        // Add o Update
        foreach (var i in incoming)
        {
            var key = (i.WordIndex, i.BitIndex);
            if (existingByKey.TryGetValue(key, out var e))
            {
                // Update solo se il meaning è cambiato
                if (e.Meaning != i.Meaning)
                    e.Meaning = i.Meaning;
            }
            else
            {
                // Add
                await DbSet.AddAsync(new BitInterpretationEntity
                {
                    VariableId = variableId,
                    DeviceId = deviceId,
                    WordIndex = i.WordIndex,
                    BitIndex = i.BitIndex,
                    Meaning = i.Meaning
                }, cancellationToken);
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
