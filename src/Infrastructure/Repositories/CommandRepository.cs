using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Repositories;

public class CommandRepository : RepositoryBase<CommandEntity>, ICommandRepository
{
    public CommandRepository(AppDbContext context, ILogger<RepositoryBase<CommandEntity>> logger)
        : base(context, logger)
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

    public async Task<CommandEntity?> GetByNameAsync(string name,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    public async Task<CommandEntity?> GetWithDeviceStatesAsync(int id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(c => c.DeviceStates)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
