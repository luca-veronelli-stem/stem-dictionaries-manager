using Infrastructure.Entities.Auth;

namespace Infrastructure.Interfaces.Auth;

public interface IRegistrationEventRepository
{
    Task<RegistrationEventEntity> AddAsync(RegistrationEventEntity entity,
        CancellationToken ct = default);
    Task<IReadOnlyList<RegistrationEventEntity>> ListBySourceAsync(string sourceIp,
        DateTime since, CancellationToken ct = default);
}
