using CommunityToolkit.Mvvm.ComponentModel;
using Core.Enums;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista variabili standard con stato attivo/disattivo per device.
/// Se IsGloballyDisabled, la checkbox è read-only (BR-011).
/// </summary>
public partial class VariableDeviceItem : ObservableObject
{
    public int VariableId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FullAddress { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Tipo dato della variabile (per sapere se è Bitmapped → doppio click apre DeviceContext).
    /// </summary>
    public DataTypeKind DataTypeKind { get; init; }

    /// <summary>
    /// True se la variabile è Bitmapped (supporta interpretazioni bit per device).
    /// </summary>
    public bool IsBitmapped => DataTypeKind == DataTypeKind.Bitmapped;

    /// <summary>
    /// True se Variable.IsEnabled = false (deprecata globalmente).
    /// Checkbox non editabile, forzata a false (BR-009/011).
    /// </summary>
    public bool IsGloballyDisabled { get; init; }

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
