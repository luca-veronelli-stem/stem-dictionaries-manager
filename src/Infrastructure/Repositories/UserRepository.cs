using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : RepositoryBase<UserEntity>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username,
        CancellationToken cancellationToken = default)
    {
        string normalizedUsername = username.ToLowerInvariant();
        return await DbSet
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);
    }
}
