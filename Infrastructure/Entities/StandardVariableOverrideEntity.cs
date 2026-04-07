using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class StandardVariableOverrideEntity : IAuditable
{
    public int Id { get; set; }
    public int DictionaryId { get; set; }
    public int StandardVariableId { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public DictionaryEntity Dictionary { get; set; } = null!;
    public VariableEntity StandardVariable { get; set; } = null!;
}
