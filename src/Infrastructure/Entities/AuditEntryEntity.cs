using Core.Enums;

namespace Infrastructure.Entities;

/// <summary>
/// AuditEntry does NOT implement IAuditable (it already has ChangedAt and is never modified).
/// </summary>
public class AuditEntryEntity
{
    public int Id { get; set; }
    public AuditEntityType EntityType { get; set; }
    public int EntityId { get; set; }
    public AuditOperation Operation { get; set; }
    public int ChangedById { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public UserEntity ChangedBy { get; set; } = null!;
}
