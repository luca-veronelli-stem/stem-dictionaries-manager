using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GUI.Windows.Views;

/// <summary>
/// Vista per la creazione/modifica di un comando.
/// </summary>
public partial class CommandEditView : UserControl
{
    public CommandEditView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // L'inizializzazione viene gestita dal MainViewModel
    }

    /// <summary>
    /// Filtra l'input per accettare solo caratteri hex (0-9, A-F).
    /// </summary>
    private void HexTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsAsciiHexDigit);
    }
}
