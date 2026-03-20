namespace GUI.Windows.Abstractions;

/// <summary>
/// Severità del messaggio per la status bar.
/// </summary>
public enum MessageSeverity
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Servizio per mostrare messaggi nella status bar.
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// Messaggio corrente visualizzato.
    /// </summary>
    string? CurrentMessage { get; }

    /// <summary>
    /// Severità del messaggio corrente.
    /// </summary>
    MessageSeverity CurrentSeverity { get; }

    /// <summary>
    /// Mostra un messaggio nella status bar.
    /// </summary>
    /// <param name="message">Testo del messaggio.</param>
    /// <param name="severity">Severità (default: Info).</param>
    /// <param name="autoHideSeconds">Secondi dopo i quali nascondere (0 = permanente).</param>
    void Show(string message, MessageSeverity severity = MessageSeverity.Info, int autoHideSeconds = 5);

    /// <summary>
    /// Nasconde il messaggio corrente.
    /// </summary>
    void Clear();

    /// <summary>
    /// Evento sollevato quando cambia il messaggio.
    /// </summary>
    event EventHandler? MessageChanged;
}
