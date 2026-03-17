using Infrastructure.Interfaces;

namespace Infrastructure.Entities;

public class BoardTypeEntity : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FirmwareType { get; set; }
    
    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<BoardEntity> Boards { get; set; } = [];
    public ICollection<DictionaryEntity> Dictionaries { get; set; } = [];
}
