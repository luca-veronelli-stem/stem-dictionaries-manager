using GUI.Windows.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per le impostazioni dell'applicazione (stub).
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
