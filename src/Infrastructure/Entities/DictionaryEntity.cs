using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class DictionaryEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// True if this is the dictionary of common variables (0x00xx). Max 1 in the system (BR-004).
    /// </summary>
    public bool IsStandard { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<VariableEntity> Variables { get; set; } = [];
    public ICollection<BoardEntity> Boards { get; set; } = [];
    public ICollection<StandardVariableOverrideEntity> StandardVariableOverrides { get; set; } = [];
    public ICollection<BitInterpretationEntity> BitInterpretations { get; set; } = [];
}
