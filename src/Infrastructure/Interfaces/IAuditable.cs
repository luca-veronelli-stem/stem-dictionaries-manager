namespace Infrastructure.Interfaces;

/// <summary>
/// Interface for entities with automatic temporal tracking.
/// CreatedAt is set on INSERT, UpdatedAt on UPDATE.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
