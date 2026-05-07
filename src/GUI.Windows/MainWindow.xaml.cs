using System.Windows;

namespace GUI.Windows;

/// <summary>
/// Shell principale dell'applicazione.
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
        // Chiude l'applicazione quando la MainWindow viene chiusa
        Application.Current.Shutdown();
    }
}
