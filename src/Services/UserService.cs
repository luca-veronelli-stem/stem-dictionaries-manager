using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// User service implementation.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        _repository = repository;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        UserEntity? entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<UserEntity> entities = await _repository.GetAllAsync(ct);
        return UserMapper.ToDomainList(entities);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Username uniqueness check
        if (await UsernameExistsAsync(user.Username, ct))
        {
            throw new InvalidOperationException($"Username '{user.Username}' already exists.");
        }

        UserEntity entity = UserMapper.ToEntity(user);
        UserEntity created = await _repository.AddAsync(entity, ct);
        User result = UserMapper.ToDomain(created);

        _logger.LogInformation("Created user {UserId}", result.Id);

        return result;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        UserEntity entity = await _repository.GetByIdAsync(user.Id, ct)
            ?? throw new KeyNotFoundException(
                $"User '{user.Username}' (Id={user.Id}) not found.");

        // Username uniqueness check (if changed)
        if (!entity.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
        {
            if (await UsernameExistsAsync(user.Username, ct))
            {
                throw new InvalidOperationException($"Username '{user.Username}' already exists.");
            }
        }

        UserMapper.UpdateEntity(entity, user);
        await _repository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _repository.DeleteAsync(id, ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        UserEntity? entity = await _repository.GetByUsernameAsync(username, ct);
        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        UserEntity? existing = await _repository.GetByUsernameAsync(username, ct);
        return existing is not null;
    }
}
