using Infrastructure.Entities;

namespace Infrastructure.Interfaces;

public interface IUserRepository : IRepository<UserEntity>
{
    Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
}
