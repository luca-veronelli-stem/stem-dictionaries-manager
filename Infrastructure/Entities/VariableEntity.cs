using Core.Enums;
using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class VariableEntity : IAuditable
{
    public int Id { get; set; }
    public int DictionaryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte AddressHigh { get; set; }
    public byte AddressLow { get; set; }
    public DataTypeKind DataTypeKind { get; set; }
    public int? DataTypeParam { get; set; }
    public string DataTypeRaw { get; set; } = string.Empty;
    public string? Format { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string? Unit { get; set; }
    public AccessMode AccessMode { get; set; }
    public string? Usage { get; set; }
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public DictionaryEntity Dictionary { get; set; } = null!;
    public ICollection<BitInterpretationEntity> BitInterpretations { get; set; } = [];
}
