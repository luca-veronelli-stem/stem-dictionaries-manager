using Core.Enums;
using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class CommandDeviceStateEntity : IAuditable
{
    public int Id { get; set; }
    public int CommandId { get; set; }
    public DeviceType DeviceType { get; set; }
    public bool IsEnabled { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public CommandEntity Command { get; set; } = null!;
}
