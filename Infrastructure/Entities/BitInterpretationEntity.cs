using Core.Enums;
using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class BitInterpretationEntity : IAuditable
{
    public int Id { get; set; }
    public int VariableId { get; set; }
    public DeviceType DeviceType { get; set; }
    public int WordIndex { get; set; }
    public int BitIndex { get; set; }
    public string Meaning { get; set; } = string.Empty;
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public VariableEntity Variable { get; set; } = null!;
}
