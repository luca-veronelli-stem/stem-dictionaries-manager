using System.Windows;

namespace GUI.Windows.Views;

/// <summary>
/// Dialog modale dark theme. Sostituisce MessageBox standard.
/// </summary>
public partial class DarkDialog : Window
{
    public bool Result { get; private set; }

    private DarkDialog()
    {
        InitializeComponent();
    }

    private void PrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = true;
        Close();
    }

    private void SecondaryButton_Click(object sender, RoutedEventArgs e)
    {
        Result = false;
        Close();
    }

    /// <summary>
    /// Mostra un dialog informativo (solo OK).
    /// </summary>
    public static void ShowInfo(string title, string message)
    {
        var dialog = CreateDialog(title, message, "OK", null);
        dialog.ShowDialog();
    }

    /// <summary>
    /// Mostra un dialog di errore (solo OK).
    /// </summary>
    public static void ShowError(string title, string message)
    {
        var dialog = CreateDialog(title, message, "OK", null);
        dialog.TitleText.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xE4, 0x00, 0x32)); // #E40032
        dialog.ShowDialog();
    }

    /// <summary>
    /// Mostra un dialog di warning (solo OK).
    /// </summary>
    public static void ShowWarning(string title, string message)
    {
        var dialog = CreateDialog(title, message, "OK", null);
        dialog.TitleText.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xFF, 0xC0, 0x4A)); // #FFC04A
        dialog.ShowDialog();
    }

    /// <summary>
    /// Mostra un dialog di conferma (Sì/No). Restituisce true se Sì.
    /// </summary>
    public static bool ShowConfirm(string title, string message)
    {
        var dialog = CreateDialog(title, message, "Sì", "No");
        dialog.ShowDialog();
        return dialog.Result;
    }

    /// <summary>
    /// Mostra un dialog OK/Annulla. Restituisce true se OK.
    /// </summary>
    public static bool ShowOkCancel(string title, string message)
    {
        var dialog = CreateDialog(title, message, "OK", "Annulla");
        dialog.ShowDialog();
        return dialog.Result;
    }

    private static DarkDialog CreateDialog(string title, string message,
        string primaryText, string? secondaryText)
    {
        var dialog = new DarkDialog
        {
            Owner = Application.Current.MainWindow
        };
        dialog.TitleText.Text = title;
        dialog.MessageText.Text = message;
        dialog.PrimaryButton.Content = primaryText;

        if (secondaryText is not null)
        {
            dialog.SecondaryButton.Content = secondaryText;
            dialog.SecondaryButton.Visibility = Visibility.Visible;
        }

        return dialog;
    }
}
