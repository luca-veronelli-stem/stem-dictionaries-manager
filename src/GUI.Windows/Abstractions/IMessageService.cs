namespace GUI.Windows.Abstractions;

/// <summary>
/// Severity of a status-bar message.
/// </summary>
public enum MessageSeverity
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Service for showing messages in the status bar.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Currently displayed message.
    /// </summary>
    string? CurrentMessage { get; }

    /// <summary>
    /// Severity of the current message.
    /// </summary>
    MessageSeverity CurrentSeverity { get; }

    /// <summary>
    /// Shows a message in the status bar.
    /// </summary>
    /// <param name="message">Message text.</param>
    /// <param name="severity">Severity (default: Info).</param>
    /// <param name="autoHideSeconds">Seconds after which the message is hidden (0 = permanent).</param>
    void Show(string message, MessageSeverity severity = MessageSeverity.Info, int autoHideSeconds = 5);

    /// <summary>
    /// Hides the current message.
    /// </summary>
    void Clear();

    /// <summary>
    /// Event raised when the message changes.
    /// </summary>
    event EventHandler? MessageChanged;
}
