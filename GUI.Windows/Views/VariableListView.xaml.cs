using System.Windows;
using System.Windows.Controls;
using GUI.Windows.ViewModels;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la lista delle variabili di un dizionario.
/// </summary>
public partial class VariableListView : UserControl
{
    public VariableListView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is VariableListViewModel viewModel)
        {
            // L'inizializzazione con dictionaryId viene fatta dal NavigationService
            // tramite il parametro ParentId
            var parameter = App.Services.GetService(typeof(GUI.Windows.Abstractions.INavigationService)) 
                as GUI.Windows.Abstractions.INavigationService;
            // Il ViewModel viene inizializzato esternamente
        }
    }
}
