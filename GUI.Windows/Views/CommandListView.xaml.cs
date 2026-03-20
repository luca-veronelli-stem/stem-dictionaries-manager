using GUI.Windows.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la lista dei comandi protocollo.
/// </summary>
public partial class CommandListView : UserControl
{
    public CommandListView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CommandListViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
