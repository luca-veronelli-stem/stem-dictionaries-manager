using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// User service.
/// </summary>
public interface IUserService
{
    // === Base CRUD ===

    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    // === Specific queries ===

    /// <summary>
    /// Looks up a user by username (case-insensitive).
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a username already exists.
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
}
