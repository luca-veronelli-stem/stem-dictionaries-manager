namespace Core.Models;

/// <summary>
/// System user.
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
    /// Factory method to reconstruct from the DB.
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
