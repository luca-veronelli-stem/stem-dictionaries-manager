namespace Core.Models;

/// <summary>
/// Per-dictionary override of a standard variable.
/// Every non-standard dictionary inherits ALL variables from the Standard template.
/// The override allows changing IsEnabled and Description for that context.
/// If absent → the standard variable uses the template values.
/// BR-010: max 1 per (DictionaryId, StandardVariableId).
/// BR-011: override IsEnabled=true forbidden when Variable.IsEnabled=false.
/// </summary>
public class StandardVariableOverride
{
    public int Id { get; private set; }

    /// <summary>FK to Dictionary (non-standard).</summary>
    public int DictionaryId { get; private set; }

    /// <summary>FK to Variable (in the Standard dictionary).</summary>
    public int StandardVariableId { get; private set; }

    /// <summary>Override of Variable.IsEnabled for this dictionary.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Optional description override (null = use template).</summary>
    public string? Description { get; private set; }

    public StandardVariableOverride(int dictionaryId, int standardVariableId,
        bool isEnabled, string? description = null)
    {
        DictionaryId = dictionaryId;
        StandardVariableId = standardVariableId;
        IsEnabled = isEnabled;
        Description = description;
    }

    /// <summary>
    /// Factory method to reconstruct from the DB.
    /// </summary>
    public static StandardVariableOverride Restore(int id, int dictionaryId,
        int standardVariableId, bool isEnabled, string? description)
    {
        var svo = new StandardVariableOverride(dictionaryId, standardVariableId,
            isEnabled, description)
        {
            Id = id
        };
        return svo;
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
    public void SetDescription(string? description) => Description = description;
}
