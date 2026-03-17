using Core.Enums;
using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IBoardRepository : IRepository<BoardEntity>
{
    Task<IReadOnlyList<BoardEntity>> GetByDeviceTypeAsync(DeviceType deviceType, 
        CancellationToken cancellationToken = default);
    Task<BoardEntity?> GetByProtocolAddressAsync(uint protocolAddress, 
        CancellationToken cancellationToken = default);
}
