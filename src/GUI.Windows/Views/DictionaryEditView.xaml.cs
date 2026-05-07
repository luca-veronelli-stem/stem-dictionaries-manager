using System.Windows;
using System.Windows.Controls;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;

namespace GUI.Windows.Views;

/// <summary>
/// View for creating/editing a dictionary.
/// Minimal code-behind - only triggers initialization.
/// </summary>
public partial class DictionaryEditView : UserControl
{
    public DictionaryEditView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialize the ViewModel with the navigation parameters
        if (DataContext is DictionaryEditViewModel viewModel)
        {
            // Retrieve the navigation parameter
            var navigationService = App.Services.GetService(typeof(INavigationService)) as Services.NavigationService;
            int? entityId = navigationService?.CurrentParameter?.EntityId;

            await viewModel.InitializeAsync(entityId);
        }
    }
}
