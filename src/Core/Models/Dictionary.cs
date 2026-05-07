namespace Core.Models;

/// <summary>
/// Dictionary: a set of variables.
/// IsStandard = variables common to all devices (0x00xx), max 1 in the system (BR-004).
/// The semantics (Standard/Dedicated/Shared/Orphan) is derived from the Boards that reference it.
/// </summary>
public class Dictionary
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    /// <summary>
    /// True if this is the dictionary of common variables (0x00xx). Max 1 in the system.
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
    /// Factory method to reconstruct from the DB.
    /// Validates address uniqueness (fail-fast on corrupted data).
    /// </summary>
    public static Dictionary Restore(int id, string name, string? description,
        bool isStandard, IEnumerable<Variable> variables)
    {
        var dictionary = new Dictionary(name, description, isStandard)
        {
            Id = id
        };

        var varList = variables.ToList();
        var duplicates = varList.GroupBy(v => v.FullAddress).Where(g => g.Count() > 1).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                $"Duplicate FullAddress found in dictionary '{name}': " +
                string.Join(", ", duplicates.Select(g => $"0x{g.Key:X4}")));
        }

        dictionary._variables.AddRange(varList);
        return dictionary;
    }

    public void AddVariable(Variable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        // Check address uniqueness
        if (_variables.Any(v => v.FullAddress == variable.FullAddress))
        {
            throw new InvalidOperationException(
                $"Variable with address 0x{variable.FullAddress:X4} already exists in dictionary.");
        }

        _variables.Add(variable);
    }

    public void RemoveVariable(Variable variable)
    {
        _variables.Remove(variable);
    }

    /// <summary>
    /// Verifies that all addresses are unique.
    /// </summary>
    public bool HasUniqueAddresses()
    {
        var addresses = _variables.Select(v => v.FullAddress).ToList();
        return addresses.Count == addresses.Distinct().Count();
    }
}
