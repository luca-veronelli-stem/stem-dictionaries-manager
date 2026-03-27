using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace GUI.Windows.Views;

public partial class DeviceEditView : UserControl
{
    public DeviceEditView()
    {
        InitializeComponent();
    }

    private void IntTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }
}
