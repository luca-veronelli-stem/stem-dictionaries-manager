namespace GUI.Windows.Abstractions;

/// <summary>
/// Result of a confirmation dialog.
/// </summary>
public enum DialogResult
{
    Ok,
    Cancel,
    Yes,
    No
}

/// <summary>
/// Service for displaying modal dialogs.
/// Decouples ViewModels from the WPF UI.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog with Yes/No buttons.
    /// </summary>
    Task<DialogResult> ShowConfirmAsync(string title, string message);

    /// <summary>
    /// Shows a confirmation dialog with Ok/Cancel buttons.
    /// </summary>
    Task<DialogResult> ShowOkCancelAsync(string title, string message);

    /// <summary>
    /// Shows an error message.
    /// </summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows an informational message.
    /// </summary>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows a warning message.
    /// </summary>
    Task ShowWarningAsync(string title, string message);
}
