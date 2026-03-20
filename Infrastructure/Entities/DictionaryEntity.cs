using Core.Enums;
using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class DictionaryEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DeviceType? DeviceType { get; set; }
    public int? BoardTypeId { get; set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public BoardTypeEntity? BoardType { get; set; }
    public ICollection<VariableEntity> Variables { get; set; } = [];
}
