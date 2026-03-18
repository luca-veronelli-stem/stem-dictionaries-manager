namespace GUI.Windows.Abstractions;

/// <summary>
/// Risultato di un dialog di conferma.
/// </summary>
public enum DialogResult
{
    Ok,
    Cancel,
    Yes,
    No
}

/// <summary>
/// Servizio per visualizzare dialog modali.
/// Disaccoppia i ViewModel dalla UI di WPF.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Mostra un dialog di conferma con pulsanti Sì/No.
    /// </summary>
    Task<DialogResult> ShowConfirmAsync(string title, string message);
    
    /// <summary>
    /// Mostra un dialog di conferma con pulsanti Ok/Annulla.
    /// </summary>
    Task<DialogResult> ShowOkCancelAsync(string title, string message);
    
    /// <summary>
    /// Mostra un messaggio di errore.
    /// </summary>
    Task ShowErrorAsync(string title, string message);
    
    /// <summary>
    /// Mostra un messaggio informativo.
    /// </summary>
    Task ShowInfoAsync(string title, string message);
    
    /// <summary>
    /// Mostra un messaggio di warning.
    /// </summary>
    Task ShowWarningAsync(string title, string message);
}
