using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GUI.Windows.Converters;

/// <summary>
/// Converte un booleano in Visibility.
/// true -> Visible, false -> Collapsed
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Se parameter è "Inverse", inverte la logica
            if (parameter is string param && param == "Inverse")
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        return false;
    }
}

/// <summary>
/// Inverte un booleano.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;
        return false;
    }
}

/// <summary>
/// Converte null in Visibility.
/// null -> Collapsed, not null -> Visible
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isVisible = value is not null;
        
        // Se parameter è "Inverse", inverte la logica
        if (parameter is string param && param == "Inverse")
            isVisible = !isVisible;
            
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
