using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class CommandEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte CodeHigh { get; set; }
    public byte CodeLow { get; set; }
    public bool IsResponse { get; set; }

    /// <summary>
    /// Structured command parameters. Persisted to the <c>ParametersJson</c>
    /// column as a JSON array via an EF Core typed value conversion
    /// (see <c>AppDbContext.OnModelCreating</c>).
    /// </summary>
    public List<string> Parameters { get; set; } = [];

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<CommandDeviceStateEntity> DeviceStates { get; set; } = [];
}
