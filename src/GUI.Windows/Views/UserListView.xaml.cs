using System.Windows;
using System.Windows.Controls;
using GUI.Windows.ViewModels;

namespace GUI.Windows.Views;

/// <summary>
/// View for the user list with an inline creation form.
/// </summary>
public partial class UserListView : UserControl
{
    public UserListView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is UserListViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }
}
