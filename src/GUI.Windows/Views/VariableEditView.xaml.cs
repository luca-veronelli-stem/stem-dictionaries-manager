using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GUI.Windows.Views;

/// <summary>
/// View for creating/editing a variable.
/// </summary>
public partial class VariableEditView : UserControl
{
    public VariableEditView()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
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

    /// <summary>
    /// Filters input to accept only positive integers (0-9).
    /// </summary>
    private void IntTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsAsciiDigit);
    }

    /// <summary>
    /// Filters input to accept only decimal numbers (0-9, '.', ',', '-').
    /// </summary>
    private void DoubleTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(c => char.IsAsciiDigit(c) || c is '.' or ',' or '-');
    }
}
