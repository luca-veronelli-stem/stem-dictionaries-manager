using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class CommandEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte CodeHigh { get; set; }
    public byte CodeLow { get; set; }
    public bool IsResponse { get; set; }
    public string ParametersJson { get; set; } = "[]";
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<CommandDeviceStateEntity> DeviceStates { get; set; } = [];
}
