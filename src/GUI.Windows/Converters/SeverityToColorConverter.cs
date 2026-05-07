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
    private static readonly SolidColorBrush SuccessBrush = new(Color.FromRgb(0x98, 0xD8, 0x01));
    private static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(0xFF, 0xC0, 0x4A));
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(0xE4, 0x00, 0x32));

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
