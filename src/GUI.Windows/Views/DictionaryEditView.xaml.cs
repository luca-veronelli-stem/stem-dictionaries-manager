using System.Windows.Controls;

namespace GUI.Windows.Views;

/// <summary>
/// View for creating/editing a dictionary. The ViewModel is created and
/// initialized by MainViewModel during navigation (InitializeViewModelAsync),
/// so the code-behind only wires up the compiled XAML.
/// </summary>
public partial class DictionaryEditView : UserControl
{
    public DictionaryEditView()
    {
        InitializeComponent();
    }
}
