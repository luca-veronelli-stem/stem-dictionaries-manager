namespace Core.Models;

/// <summary>
/// Dizionario: set di variabili.
/// IsStandard = variabili comuni a tutti (0x00xx), max 1 nel sistema (BR-004).
/// La semantica (Standard/Dedicated/Shared/Orphan) è derivata dai Board che lo referenziano.
/// </summary>
public class Dictionary
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    /// <summary>
    /// True se è il dizionario delle variabili comuni (0x00xx). Max 1 nel sistema.
    /// </summary>
    public bool IsStandard { get; private set; }

    private readonly List<Variable> _variables = [];
    public IReadOnlyList<Variable> Variables => _variables.AsReadOnly();

    public Dictionary(string name, string? description = null, bool isStandard = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
        IsStandard = isStandard;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Dictionary Restore(int id, string name, string? description,
        bool isStandard, IEnumerable<Variable> variables)
    {
        var dictionary = new Dictionary(name, description, isStandard)
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
