using Core.Enums;

namespace Core.Models;

/// <summary>
/// Dizionario: set di variabili.
/// 3 semantiche: Standard (null,null), Periferica condivisa (null,BT), Dedicato (DT,BT).
/// Combinazione invalida: (DT, null) — se c'è il device, serve il BoardType.
/// </summary>
public class Dictionary
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    /// <summary>
    /// Tipo dispositivo associato. Null per Standard o periferica condivisa.
    /// </summary>
    public DeviceType? DeviceType { get; private set; }

    /// <summary>
    /// BoardType associato. Null solo per dizionario Standard.
    /// </summary>
    public BoardType? BoardType { get; private set; }

    private readonly List<Variable> _variables = [];
    public IReadOnlyList<Variable> Variables => _variables.AsReadOnly();

    public Dictionary(string name, DeviceType? deviceType = null, BoardType? boardType = null, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Dedicato richiede BoardType: (DeviceType, null) è invalido.
        if (deviceType.HasValue && boardType is null)
            throw new ArgumentException("DeviceType requires a BoardType. Use (null, null) for Standard or (null, BoardType) for shared.");

        Name = name;
        DeviceType = deviceType;
        BoardType = boardType;
        Description = description;
    }

    /// <summary>
    /// Factory method per ricostruire da DB.
    /// </summary>
    public static Dictionary Restore(int id, string name, DeviceType? deviceType, BoardType? boardType,
        string? description, IEnumerable<Variable> variables)
    {
        var dictionary = new Dictionary(name, deviceType, boardType, description)
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
