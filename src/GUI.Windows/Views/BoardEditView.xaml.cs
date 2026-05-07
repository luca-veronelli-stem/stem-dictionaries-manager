using System.Windows;
using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// View for creating/editing a board.
/// </summary>
public partial class BoardEditView : UserControl
{
    public BoardEditView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialization is handled by MainViewModel
    }
}
