namespace Core.Models;

/// <summary>
/// Override per-dizionario di una variabile standard.
/// Ogni dizionario non-standard eredita TUTTE le variabili del template Standard.
/// L'override permette di cambiare IsEnabled e Description per quel contesto.
/// Se assente → la variabile standard usa i valori del template.
/// BR-010: max 1 per (DictionaryId, StandardVariableId).
/// BR-011: override IsEnabled=true vietato se Variable.IsEnabled=false.
/// </summary>
public class StandardVariableOverride
{
    public int Id { get; private set; }

    /// <summary>FK verso Dictionary (non-standard).</summary>
    public int DictionaryId { get; private set; }

    /// <summary>FK verso Variable (nel dizionario Standard).</summary>
    public int StandardVariableId { get; private set; }

    /// <summary>Override di Variable.IsEnabled per questo dizionario.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Override opzionale della descrizione (null = usa template).</summary>
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
    /// Factory method per ricostruire da DB.
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
