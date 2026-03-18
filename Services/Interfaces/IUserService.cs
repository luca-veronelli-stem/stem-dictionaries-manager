using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione utenti.
/// </summary>
public interface IUserService
{
    // === CRUD Base ===
    
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<User> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    
    // === Query Specifiche ===
    
    /// <summary>
    /// Cerca utente per username (case-insensitive).
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    
    /// <summary>
    /// Verifica se un username esiste già.
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);
}
