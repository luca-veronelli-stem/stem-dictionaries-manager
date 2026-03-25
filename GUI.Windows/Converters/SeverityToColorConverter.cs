using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GUI.Windows.Abstractions;

namespace GUI.Windows.Converters;

/// <summary>
/// Converte MessageSeverity in un colore di sfondo per la status bar.
/// </summary>
public class SeverityToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(0x3C, 0x3C, 0x3C));
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(0x2E, 0x7D, 0x32));
    private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(0xE6, 0x5C, 0x00));
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(0xC6, 0x28, 0x28));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is MessageSeverity severity ? severity switch
        {
            MessageSeverity.Success => SuccessBrush,
            MessageSeverity.Warning => WarningBrush,
            MessageSeverity.Error => ErrorBrush,
            _ => InfoBrush
        } : InfoBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
