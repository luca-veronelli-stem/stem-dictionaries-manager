using CommunityToolkit.Mvvm.ComponentModel;

namespace GUI.Windows.ViewModels;

public partial class BitInterpretationItem : ObservableObject
{
    [ObservableProperty]
    private int _wordIndex;

    [ObservableProperty]
    private int _bitIndex;

    [ObservableProperty]
    private string _meaning = string.Empty;
}
