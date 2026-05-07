using System.Windows;

namespace GUI.Windows.Views;

/// <summary>
/// Modal dark-theme dialog. Replaces the standard MessageBox.
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
    /// Shows an informational dialog (OK only).
    /// </summary>
    public static void ShowInfo(string title, string message)
    {
        DarkDialog dialog = CreateDialog(title, message, "OK", null);
        dialog.ShowDialog();
    }

    /// <summary>
    /// Shows an error dialog (OK only).
    /// </summary>
    public static void ShowError(string title, string message)
    {
        DarkDialog dialog = CreateDialog(title, message, "OK", null);
        dialog.TitleText.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xE4, 0x00, 0x32)); // #E40032
        dialog.ShowDialog();
    }

    /// <summary>
    /// Shows a warning dialog (OK only).
    /// </summary>
    public static void ShowWarning(string title, string message)
    {
        DarkDialog dialog = CreateDialog(title, message, "OK", null);
        dialog.TitleText.Foreground = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xFF, 0xC0, 0x4A)); // #FFC04A
        dialog.ShowDialog();
    }

    /// <summary>
    /// Shows a confirmation dialog (Yes/No). Returns true on Yes.
    /// </summary>
    public static bool ShowConfirm(string title, string message)
    {
        DarkDialog dialog = CreateDialog(title, message, "Yes", "No");
        dialog.ShowDialog();
        return dialog.Result;
    }

    /// <summary>
    /// Shows an OK/Cancel dialog. Returns true on OK.
    /// </summary>
    public static bool ShowOkCancel(string title, string message)
    {
        DarkDialog dialog = CreateDialog(title, message, "OK", "Cancel");
        dialog.ShowDialog();
        return dialog.Result;
    }

    private static DarkDialog CreateDialog(string title, string message,
        string primaryText, string? secondaryText)
    {
        var dialog = new DarkDialog();

        // Owner = MainWindow when available (not during startup)
        Window? mainWindow = Application.Current.MainWindow;
        if (mainWindow is not null && mainWindow != dialog)
        {
            dialog.Owner = mainWindow;
        }
        else
        {
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

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
