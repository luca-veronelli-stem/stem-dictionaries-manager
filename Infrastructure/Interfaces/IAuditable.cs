namespace Infrastructure.Interfaces;

/// <summary>
/// Interface per entities con tracking temporale automatico.
/// CreatedAt viene settato su INSERT, UpdatedAt su UPDATE.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
