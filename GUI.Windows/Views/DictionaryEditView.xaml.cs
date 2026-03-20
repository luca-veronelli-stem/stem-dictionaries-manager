using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// View per la creazione/modifica di un dizionario.
/// Code-behind minimale - solo per trigger inizializzazione.
/// </summary>
public partial class DictionaryEditView : UserControl
{
    public DictionaryEditView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Inizializza il ViewModel con i parametri di navigazione
        if (DataContext is DictionaryEditViewModel viewModel)
        {
            // Recupera il parametro di navigazione
            var navigationService = App.Services.GetService(typeof(INavigationService)) as Services.NavigationService;
            var entityId = navigationService?.CurrentParameter?.EntityId;

            await viewModel.InitializeAsync(entityId);
        }
    }
}
