using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GUI.Windows.Converters;

/// <summary>
/// Converts a boolean to Visibility.
/// true -> Visible, false -> Collapsed
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // If parameter is "Inverse", invert the logic
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
/// Inverts a boolean.
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
/// Converts null to Visibility.
/// null -> Collapsed, not null -> Visible
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = value is not null;

        // If parameter is "Inverse", invert the logic
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
/// Converts a boolean to a Brush for the border of fields in error.
/// true (invalid) → red, false (valid) → default border color.
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
/// Converts (isVisible, isExpanded) into a GridLength for rows hosting an Expander.
/// Visible + expanded → Star (fills the available space),
/// visible + collapsed → Auto (header only),
/// not visible → Auto (effectively 0 when the Expander is Collapsed).
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
