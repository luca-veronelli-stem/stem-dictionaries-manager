using CommunityToolkit.Mvvm.ComponentModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la DataGrid dei parametri comando.
/// Ogni riga rappresenta un parametro con indice, dimensione e descrizione.
/// Serializzato in Command.Parameters come "size|description".
/// </summary>
public partial class CommandParameterItem : ObservableObject
{
    /// <summary>
    /// Indice del parametro (0-based, auto-incrementato).
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Display per la UI: "1", "2", etc.
    /// </summary>
    public string IndexDisplay => $"{Index + 1}";

    [ObservableProperty]
    private string _sizeBytes = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Serializza nel formato "size|description" per Command.Parameters.
    /// </summary>
    public string Serialize() => $"{SizeBytes}|{Description}";

    /// <summary>
    /// Deserializza da stringa "size|description" con fallback legacy.
    /// </summary>
    public static CommandParameterItem Deserialize(int index, string raw)
    {
        var parts = raw.Split('|', 2);
        return parts.Length == 2
            ? new CommandParameterItem { Index = index, SizeBytes = parts[0], Description = parts[1] }
            : new CommandParameterItem { Index = index, SizeBytes = "", Description = raw };
    }
}
