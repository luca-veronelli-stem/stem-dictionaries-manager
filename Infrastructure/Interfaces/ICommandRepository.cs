using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface ICommandRepository : IRepository<CommandEntity>
{
    Task<CommandEntity?> GetByCodeAsync(byte codeHigh, byte codeLow, bool isResponse, 
        CancellationToken cancellationToken = default);
    Task<CommandEntity?> GetWithDeviceStatesAsync(int id, CancellationToken cancellationToken = default);
}
