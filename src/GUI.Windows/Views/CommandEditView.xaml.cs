using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GUI.Windows.Views;

/// <summary>
/// View for creating/editing a command.
/// </summary>
public partial class CommandEditView : UserControl
{
    public CommandEditView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Initialization is handled by MainViewModel
    }

    /// <summary>
    /// Filters input to accept only hex characters (0-9, A-F).
    /// </summary>
    private void HexTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsAsciiHexDigit);
    }
}
