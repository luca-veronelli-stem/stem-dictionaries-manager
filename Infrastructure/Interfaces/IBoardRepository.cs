using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBoardRepository : IRepository<BoardEntity>
{
    Task<IReadOnlyList<BoardEntity>> GetByDeviceIdAsync(int deviceId,
        CancellationToken cancellationToken = default);
    Task<BoardEntity?> GetByProtocolAddressAsync(uint protocolAddress,
        CancellationToken cancellationToken = default);
}
