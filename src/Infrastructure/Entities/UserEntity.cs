using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class UserEntity : IAuditable
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<AuditEntryEntity> AuditEntries { get; set; } = [];
}
