using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class DeviceEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MachineCode { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<BoardEntity> Boards { get; set; } = [];
}
