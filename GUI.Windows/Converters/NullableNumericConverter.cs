using System.Globalization;
using System.Windows.Data;

namespace GUI.Windows.Converters;

/// <summary>
/// Converter per binding di int? a TextBox. Gestisce stringhe vuote e non valide.
/// </summary>
public class NullableIntConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return null;

        return int.TryParse(str, out var result) ? result : Binding.DoNothing;
    }
}

/// <summary>
/// Converter per binding di double? a TextBox. Gestisce stringhe vuote e non valide.
/// </summary>
public class NullableDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return null;

        // Supporta sia '.' che ',' come separatore decimale
        str = str.Replace(',', '.');
        return double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) 
            ? result 
            : Binding.DoNothing;
    }
}
