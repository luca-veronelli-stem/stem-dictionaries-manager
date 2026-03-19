using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la creazione/modifica di una variabile.
/// </summary>
public partial class VariableEditView : UserControl
{
    public VariableEditView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // L'inizializzazione viene gestita dal MainViewModel
    }
}
