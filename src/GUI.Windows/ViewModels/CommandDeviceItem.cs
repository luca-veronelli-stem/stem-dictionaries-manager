using CommunityToolkit.Mvvm.ComponentModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item for the command list with per-device enabled/disabled state.
/// </summary>
public partial class CommandDeviceItem : ObservableObject
{
    public int CommandId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FullCode { get; init; } = string.Empty;
    public string TypeDisplay { get; init; } = string.Empty;

    /// <summary>
    /// Original state loaded from the DB (used to track modifications).
    /// </summary>
    public bool OriginalIsEnabled { get; init; }

    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>
    /// True if the state has been modified relative to the original.
    /// </summary>
    public bool HasChanged => IsEnabled != OriginalIsEnabled;
}
