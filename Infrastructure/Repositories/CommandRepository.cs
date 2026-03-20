using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CommandRepository : RepositoryBase<CommandEntity>, ICommandRepository
{
    public CommandRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CommandEntity?> GetByCodeAsync(byte codeHigh, byte codeLow, bool isResponse,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c =>
                c.CodeHigh == codeHigh &&
                c.CodeLow == codeLow &&
                c.IsResponse == isResponse,
                cancellationToken);
    }

    public async Task<CommandEntity?> GetWithDeviceStatesAsync(int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.DeviceStates)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
