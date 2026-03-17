namespace Core.Models;

/// <summary>
/// Dizionario: set di variabili per un tipo di scheda.
/// </summary>
public class Dictionary
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    
    /// <summary>
    /// BoardType associato. Null per dizionario "Standard" (condiviso).
    /// </summary>
    public BoardType? BoardType { get; private set; }

    private readonly List<Variable> _variables = [];
    public IReadOnlyList<Variable> Variables => _variables.AsReadOnly();

    public Dictionary(string name, BoardType? boardType = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        BoardType = boardType;
        Description = description;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Dictionary Restore(int id, string name, BoardType? boardType, 
        string? description, IEnumerable<Variable> variables)
    {
        var dictionary = new Dictionary(name, boardType, description)
        {
            Id = id
        };
        dictionary._variables.AddRange(variables);
        return dictionary;
    }

    public void AddVariable(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        
        // Verifica unicità indirizzo
        if (_variables.Any(v => v.FullAddress == variable.FullAddress))
            throw new InvalidOperationException(
                $"Variable with address 0x{variable.FullAddress:X4} already exists in dictionary.");

        _variables.Add(variable);
    }

    public void RemoveVariable(Variable variable)
    {
        _variables.Remove(variable);
    }

    /// <summary>
    /// Verifica che tutti gli indirizzi siano univoci.
    /// </summary>
    public bool HasUniqueAddresses()
    {
        var addresses = _variables.Select(v => v.FullAddress).ToList();
        return addresses.Count == addresses.Distinct().Count();
    }
}
