using Core.Enums;
using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class BoardEntity : IAuditable
{
    public int Id { get; set; }
    public DeviceType DeviceType { get; set; }
    public int BoardTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BoardNumber { get; set; }
    public string? PartNumber { get; set; }
    public uint ProtocolAddress { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public BoardTypeEntity BoardType { get; set; } = null!;
}
