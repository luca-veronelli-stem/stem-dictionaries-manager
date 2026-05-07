using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class BoardEntity : IAuditable
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FirmwareType { get; set; }
    public int BoardNumber { get; set; }
    public string? PartNumber { get; set; }
    public uint ProtocolAddress { get; set; }

    /// <summary>
    /// True if this is the device's primary board. Max 1 per Device.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Associated dictionary. Null = board without its own dictionary.
    /// </summary>
    public int? DictionaryId { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public DeviceEntity Device { get; set; } = null!;
    public DictionaryEntity? Dictionary { get; set; }
}
