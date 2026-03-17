namespace Core.Models;

/// <summary>
/// Utente del sistema.
/// </summary>
public class User
{
    public int Id { get; private set; }
    public string Username { get; private set; }
    public string DisplayName { get; private set; }

    public User(string username, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Username = username.ToLowerInvariant();
        DisplayName = displayName;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static User Restore(int id, string username, string displayName)
    {
        var user = new User(username, displayName)
        {
            Id = id
        };
        return user;
    }
}
