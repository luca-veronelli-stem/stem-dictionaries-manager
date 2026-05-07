using System.Windows;
using System.Windows.Controls;
using GUI.Windows.ViewModels;

namespace GUI.Windows.Views;

/// <summary>
/// View for the application settings (stub).
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
