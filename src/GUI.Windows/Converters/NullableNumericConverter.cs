using System.Globalization;
using System.Windows.Data;

namespace GUI.Windows.Converters;

/// <summary>
/// Two-way converter binding <c>int?</c> to a <c>TextBox</c>. Empty / invalid strings round-trip to <c>null</c>.
/// </summary>
public class NullableIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IFormattable f
            ? f.ToString(null, culture)
            : value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        return int.TryParse(str, out int result) ? result : Binding.DoNothing;
    }
}

/// <summary>
/// Two-way converter binding <c>double?</c> to a <c>TextBox</c>. Empty / invalid strings round-trip to <c>null</c>.
/// </summary>
public class NullableDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is IFormattable f
            ? f.ToString(null, culture)
            : value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
        {
            return null;
        }

        // Accept both '.' and ',' as decimal separators (mixed input from EU keyboards).
        str = str.Replace(',', '.');
        return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double result)
            ? result
            : Binding.DoNothing;
    }
}
