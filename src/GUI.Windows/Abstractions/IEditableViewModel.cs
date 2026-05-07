namespace GUI.Windows.Abstractions;

/// <summary>
/// Interface for ViewModels with a modified state (edit forms).
/// Used by MainViewModel to warn on backward navigation when there are unsaved changes.
/// </summary>
public interface IEditableViewModel
{
    /// <summary>
    /// True if the form has unsaved changes.
    /// </summary>
    bool HasChanges { get; }
}
