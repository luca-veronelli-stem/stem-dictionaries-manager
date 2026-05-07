using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la creazione/modifica di una scheda.
/// </summary>
public partial class BoardEditView : UserControl
{
    public BoardEditView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // L'inizializzazione viene gestita dal MainViewModel
    }
}
