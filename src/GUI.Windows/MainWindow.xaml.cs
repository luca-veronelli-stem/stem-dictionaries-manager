using System.Windows;

namespace GUI.Windows;

/// <summary>
/// Main application shell.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Shut down the application when MainWindow is closed
        Application.Current.Shutdown();
    }
}
