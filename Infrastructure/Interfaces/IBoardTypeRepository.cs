using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBoardTypeRepository : IRepository<BoardTypeEntity>
{
    Task<BoardTypeEntity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<BoardTypeEntity?> GetByFirmwareTypeAsync(int firmwareType, CancellationToken cancellationToken = default);
}
