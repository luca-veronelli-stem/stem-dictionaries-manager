using System.Windows;
using System.Windows.Controls;
using GUI.Windows.ViewModels;

namespace GUI.Windows.Views;

/// <summary>
/// View for the dictionary list.
/// Minimal code-behind - only triggers initialization.
/// </summary>
public partial class DictionaryListView : UserControl
{
    public DictionaryListView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Load data when the view is displayed
        if (DataContext is DictionaryListViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
