using GUI.Windows.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// View per la lista dei dizionari.
/// Code-behind minimale - solo per trigger inizializzazione.
/// </summary>
public partial class DictionaryListView : UserControl
{
    public DictionaryListView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Carica i dati quando la view viene mostrata
        if (DataContext is DictionaryListViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
