using System.Windows;
using System.Windows.Controls;
using GUI.Windows.ViewModels;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la lista degli utenti con form di creazione inline.
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
            await viewModel.InitializeAsync();
        }
    }
}
