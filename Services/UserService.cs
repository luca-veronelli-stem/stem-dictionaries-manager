using Core.Models;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione utenti.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _repository;

    public UserService(IUserRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetAllAsync(ct);
        return UserMapper.ToDomainList(entities);
    }

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Verifica unicità username
        if (await UsernameExistsAsync(user.Username, ct))
            throw new InvalidOperationException($"Username '{user.Username}' already exists.");

        var entity = UserMapper.ToEntity(user);
        var created = await _repository.AddAsync(entity, ct);
        return UserMapper.ToDomain(created);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var entity = await _repository.GetByIdAsync(user.Id, ct)
            ?? throw new KeyNotFoundException($"User with Id {user.Id} not found.");

        // Verifica unicità username (se cambiato)
        if (!entity.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase))
        {
            if (await UsernameExistsAsync(user.Username, ct))
                throw new InvalidOperationException($"Username '{user.Username}' already exists.");
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

        var entity = await _repository.GetByUsernameAsync(username, ct);
        return entity is null ? null : UserMapper.ToDomain(entity);
    }

    public async Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var existing = await _repository.GetByUsernameAsync(username, ct);
        return existing is not null;
    }
}
