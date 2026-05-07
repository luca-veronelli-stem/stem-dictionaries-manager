using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Visible;
        }

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
        {
            return !boolValue;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

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
        bool isVisible = value is not null;

        // Se parameter è "Inverse", inverte la logica
        if (parameter is string param && param == "Inverse")
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converte un booleano in un Brush per il bordo dei campi con errore.
/// true (invalid) → rosso, false (valid) → colore bordo di default.
/// </summary>
public class BoolToErrorBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(0xE4, 0x00, 0x32));
    private static readonly SolidColorBrush NormalBrush = new(Color.FromRgb(0x55, 0x55, 0x55));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? ErrorBrush : NormalBrush;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Converte (isVisible, isExpanded) in GridLength per righe con Expander.
/// Visibile + espanso → Star (riempie lo spazio disponibile),
/// visibile + collassato → Auto (solo header),
/// non visibile → Auto (0 effettivo con Expander Collapsed).
/// </summary>
public class ExpandedRowHeightConverter : IMultiValueConverter
{
    private static readonly GridLength Star = new(1, GridUnitType.Star);

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is bool isVisible && values[1] is bool isExpanded)
        {
            return isVisible && isExpanded ? Star : GridLength.Auto;
        }

        return GridLength.Auto;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
