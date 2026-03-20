using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la creazione/modifica di un comando.
/// </summary>
public partial class CommandEditView : UserControl
{
    public CommandEditView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // L'inizializzazione viene gestita dal MainViewModel
    }
}
