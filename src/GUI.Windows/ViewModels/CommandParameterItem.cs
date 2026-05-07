using CommunityToolkit.Mvvm.ComponentModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item for the command-parameters DataGrid.
/// Each row represents a parameter with index, size and description.
/// Serialized into Command.Parameters as "size|description".
/// </summary>
public partial class CommandParameterItem : ObservableObject
{
    /// <summary>
    /// Parameter index (0-based, auto-incremented).
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// UI display: "1", "2", etc.
    /// </summary>
    public string IndexDisplay => $"{Index + 1}";

    [ObservableProperty]
    private string _sizeBytes = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Serializes into the "size|description" format for Command.Parameters.
    /// </summary>
    public string Serialize() => $"{SizeBytes}|{Description}";

    /// <summary>
    /// Deserializes from a "size|description" string with legacy fallback.
    /// </summary>
    public static CommandParameterItem Deserialize(int index, string raw)
    {
        string[] parts = raw.Split('|', 2);
        return parts.Length == 2
            ? new CommandParameterItem { Index = index, SizeBytes = parts[0], Description = parts[1] }
            : new CommandParameterItem { Index = index, SizeBytes = "", Description = raw };
    }
}
