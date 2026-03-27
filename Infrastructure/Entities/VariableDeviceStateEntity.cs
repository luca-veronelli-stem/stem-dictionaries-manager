using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class VariableDeviceStateEntity : IAuditable
{
    public int Id { get; set; }
    public int VariableId { get; set; }
    public int DeviceId { get; set; }
    public bool IsEnabled { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public VariableEntity Variable { get; set; } = null!;
}
