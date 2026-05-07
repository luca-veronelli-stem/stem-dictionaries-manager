using CommunityToolkit.Mvvm.ComponentModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista comandi con stato attivo/disattivo per device.
/// </summary>
public partial class CommandDeviceItem : ObservableObject
{
    public int CommandId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FullCode { get; init; } = string.Empty;
    public string TypeDisplay { get; init; } = string.Empty;

    /// <summary>
    /// Stato originale caricato dal DB (per tracciare modifiche).
    /// </summary>
    public bool OriginalIsEnabled { get; init; }

    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>
    /// True se lo stato è stato modificato rispetto all'originale.
    /// </summary>
    public bool HasChanged => IsEnabled != OriginalIsEnabled;
}
