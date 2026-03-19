using System.Windows;
using System.Windows.Controls;
using GUI.Windows.ViewModels;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la lista delle schede.
/// </summary>
public partial class BoardListView : UserControl
{
    public BoardListView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BoardListViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
